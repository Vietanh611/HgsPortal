using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using WebApp.Client.Services;
using WebApp.Client.Services.Auth;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Data;
using WebApp.Client.Services.Network;
using WebApp.Components;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Add antiforgery for Blazor Web App (required for interactive components)
builder.Services.AddAntiforgery();

// Register services needed for prerendering (WebAssembly components)
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITokenStorage, ServerTokenStorage>();
builder.Services.AddScoped<IMenuCacheService, ServerMenuCacheService>();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DialogService>();

// Configure HttpClient for client services
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://localhost:5032";
builder.Services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Register client-side services for prerendering support
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenRefreshService>();

builder.Services.AddScoped<ApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("ApiClient");
    var tokenStorage = sp.GetRequiredService<ITokenStorage>();
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new ApiClient(httpClient, tokenStorage, navigationManager, apiBaseUrl);
});
builder.Services.AddScoped<CoreAssetsService>();
builder.Services.AddScoped<DisplayDevicesService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? string.Empty;
        if (path.Contains("/_framework/", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
        }
        else
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    }
});
app.UseAntiforgery();
app.UseSerilogRequestLogging();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(WebApp.Client._Imports).Assembly);

app.Run();
