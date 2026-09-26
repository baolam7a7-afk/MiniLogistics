using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MiniLogistics.Web;
using MiniLogistics.Web.Services.Auth;

var builder =
    WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

builder.RootComponents.Add<HeadOutlet>(
    "head::after");

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress =
            new Uri("http://localhost:5136/")
    });

builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<TokenStorageService>();

builder.Services.AddScoped<AuthStateService>();

await builder.Build().RunAsync();