using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebApp.Client.Services.Auth;
using WebApp.Client.Services.Data;
using WebApp.Client.Services.Network;
using WebApp.Client.Services.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.API", LogLevel.None);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);

// Add Blazored.LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Read configuration from wwwroot/appsettings.json
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://localhost:5032";

// Register HttpClient factory for auth services (without handlers)
builder.Services.AddHttpClient("AuthClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Register token storage
builder.Services.AddScoped<ITokenStorage, MemoryTokenStorage>();

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

// Register HttpClient with handlers for ApiClient
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthorizationHandler>()
  .AddHttpMessageHandler<TokenRefreshHandler>();

// Register ApiClient with all its dependencies
builder.Services.AddScoped<ApiClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("ApiClient");
    var tokenStorage = sp.GetRequiredService<ITokenStorage>();
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new ApiClient(httpClient, tokenStorage, navigationManager);
});
builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DialogService>();

await builder.Build().RunAsync();
