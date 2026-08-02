using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebApp.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

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

builder.Services.AddScoped<ApiClient>();
builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<DialogService>();

await builder.Build().RunAsync();
