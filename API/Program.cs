using API.Authorization;
using API.Middleware;
using Core.Interfaces;
using Core.Interfaces.Auth;
using Core.Interfaces.Identity;
using Core.Interfaces.Notifications;
using Core.Interfaces.Operations;
using Core.Services.Auth;
using Core.Services.Identity;
using Core.Services.Notifications;
using Core.Services.Operations;
using Core.Services.Settings;
using Data.DbContexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .CreateLogger();

    builder.Host.UseSerilog();

    // Add services 

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    builder.Services.Configure<DeviceAuthSettings>(builder.Configuration.GetSection("DeviceAuthSettings"));
    builder.Services.AddSingleton<ITokenService, TokenService>();
    builder.Services.AddMemoryCache();

    builder.Services.AddDbContext<HgsDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("PortalConnection")));
    builder.Services.AddDbContext<AcdmContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ACDMconnection")));
    builder.Services.AddDbContext<FlyOpsDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("FlyOpsConnection")));


    builder.Services.AddScoped<IOrganizationUnitsService, OrganizationUnitsService>();
    builder.Services.AddScoped<IUsersService, UsersService>();
    builder.Services.AddScoped<IRolesService, RolesService>();
    builder.Services.AddScoped<ICustomerSatisfactionService, CustomerSatisfactionService>();
    builder.Services.AddScoped<IFlightService, FlightService>();
    builder.Services.AddScoped<IUserRoleService, UserRoleService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IMenuService, MenuService>();
    builder.Services.AddScoped<IUserMenuService, UserMenuService>();
    builder.Services.AddScoped<IRoleMenuService, RoleMenuService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<IAuditLogExportService, AuditLogExportService>();
    builder.Services.AddScoped<ICriticalLogService, CriticalLogService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IPermissionDelegationService, PermissionDelegationService>();
    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddScoped<IOrgScopeService, OrgScopeService>();
    builder.Services.AddScoped<ICoreAssetsService, CoreAssetsService>();
    builder.Services.AddScoped<IDisplayService, DisplayService>();
    builder.Services.AddScoped<IDevicesService, DevicesService>();

    builder.Services.AddHttpContextAccessor();
    builder.Services.Configure<CookieSettings>(builder.Configuration.GetSection("CookieSettings"));
    builder.Services.Configure<LockoutSettings>(builder.Configuration.GetSection("LockoutSettings"));
    builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
    builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));
    builder.Services.AddSingleton<OAuth2TokenProvider>();
    builder.Services.AddScoped<IMailService, MailService>();
    builder.Services.AddHostedService<API.Services.NotificationCleanupService>();
    var rateLimitSettings = builder.Configuration.GetSection("RateLimiting");

    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                //partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = rateLimitSettings.GetValue<int>("PermitLimit"),
                Window = TimeSpan.FromSeconds(rateLimitSettings.GetValue<int>("WindowInSeconds")),
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = rateLimitSettings.GetValue<int>("QueueLimit")
            }));

        options.OnRejected = async (context, cancellationToken) =>
        {
            var httpContext = context.HttpContext;

            // Ghi nhan 429 de phat hien abuse/brute-force theo partition (user/IP) —
            // day la canh bao bao mat khong nam trong AuditLogs nen phai log o day.
            using (LogContext.PushProperty("RequestId", httpContext.TraceIdentifier))
            using (LogContext.PushProperty("User", httpContext.User.Identity?.Name ?? "Anonymous"))
            using (LogContext.PushProperty("Path", httpContext.Request.Path))
            using (LogContext.PushProperty("Method", httpContext.Request.Method))
            {
                Log.Warning(
                    "Rate limit exceeded (429). Partition: {PartitionKey}, User: {User}, {Method} {Path}",
                    httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    httpContext.User.Identity?.Name ?? "Anonymous",
                    httpContext.Request.Method,
                    httpContext.Request.Path);
            }

            httpContext.Response.StatusCode = 429;
            await httpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
        };

        // Ghép cặp thiết bị: giới hạn chặt hơn GlobalLimiter để chống dò mã pairing 8 ký tự.
        options.AddPolicy("DevicePairing", context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

        // Thiết bị (kiosk) không có JWT nên phân chia rate-limit theo X-Device-Id khi có,
        // tránh một thiết bị loop ảnh hưởng các thiết bị khác chung IP.
        options.AddPolicy("device", context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.Request.Headers["X-Device-Id"].ToString()
                    ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitSettings.GetValue<int>("PermitLimit"),
                    Window = TimeSpan.FromSeconds(rateLimitSettings.GetValue<int>("WindowInSeconds")),
                    SegmentsPerWindow = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = rateLimitSettings.GetValue<int>("QueueLimit")
                }));
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    })
    .AddScheme<DeviceKeyAuthenticationSchemeOptions, DeviceKeyAuthenticationHandler>(
        "DeviceKey", options => { });
    builder.Services.AddScoped<IAuthorizationHandler, MenuPermissionHandler>();
    builder.Services.AddScoped<IAuthorizationMiddlewareResultHandler, MenuAuthorizationResultHandler>();
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new MenuPermissionRequirement())
            .Build();
    });
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
    });
    // Add CORS policy configured via appsettings
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:5201" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "HGS Portal API",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Nhập JWT Token",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    var storageSettings = builder.Configuration.GetSection("Storage").Get<StorageSettings>() ?? new StorageSettings();
    var avatarPhysicalPath = Path.Combine(app.Environment.ContentRootPath, storageSettings.AvatarDirectory);
    Directory.CreateDirectory(avatarPhysicalPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(avatarPhysicalPath),
        RequestPath = "/uploads/avatars"
    });

    app.UseRateLimiter();
    // Add Serilog request logging
    app.Use(async (context, next) =>
    {
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        using (LogContext.PushProperty("User", context.User.Identity?.Name ?? "Anonymous"))
        using (LogContext.PushProperty("Method", context.Request.Method))
        using (LogContext.PushProperty("Path", context.Request.Path))
        {
            await next();
        }
    });
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UsePathBase("/ApiCore");
    app.UseCors("CorsPolicy");
    app.UseAntiforgery();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}