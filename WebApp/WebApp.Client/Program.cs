using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebApp.Client.Services;
using WebApp.Client.Services.Auth;
using WebApp.Client.Services.Components;
using WebApp.Client.Services.Data;
using WebApp.Client.Services.Network;
using WebApp.Client.Services.Notification;
using WebApp.Client.Services.Operation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
// Add Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Read configuration from wwwroot/appsettings.json
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl");
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException(
        "ApiBaseUrl is not configured.");
}

// Register HttpClient factory for auth services (without handlers)
builder.Services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CredentialsHandler>();

// Register token storage
builder.Services.AddScoped<ITokenStorage, TokenStorage>();

// Register menu cache service
builder.Services.AddScoped<IMenuCacheService, MenuCacheService>();

// Register auth services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenRefreshService>();
builder.Services.AddScoped<JwtTokenService>();

// Register AuthenticationStateProvider
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// Register DelegatingHandlers
builder.Services.AddScoped<AuthorizationHandler>();
builder.Services.AddScoped<TokenRefreshHandler>();
builder.Services.AddScoped<CredentialsHandler>();

// Register HttpClient with handlers for ApiClient.
// Thứ tự quan trọng: TokenRefreshHandler chạy TRƯỚC (outermost) để refresh token trong storage
// trước, rồi AuthorizationHandler (inner) mới gắn Bearer header từ token ĐÃ tươi. Nếu ngược lại,
// header mang token cũ/hết hạn trong khi refresh chỉ cập nhật storage → server trả 401.
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<TokenRefreshHandler>()
  .AddHttpMessageHandler<AuthorizationHandler>();

// Register ApiClient with all its dependencies
builder.Services.AddScoped<ApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("ApiClient");
    var tokenStorage = sp.GetRequiredService<ITokenStorage>();
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new ApiClient(httpClient, tokenStorage, navigationManager, apiBaseUrl);
});
builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<CoreAssetsService>();
builder.Services.AddScoped<DevicesService>();
builder.Services.AddScoped<KioskDeviceConfigService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<NotificationPollingService>();

await builder.Build().RunAsync();
