using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebApp.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.API", LogLevel.None);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
// Read configuration from wwwroot/appsettings.json
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? "http://localhost:5032";

builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("API");
});

builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DialogService>();

await builder.Build().RunAsync();
