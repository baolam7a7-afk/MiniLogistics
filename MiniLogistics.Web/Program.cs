using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MiniLogistics.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// =====================================================
// ROOT COMPONENTS
// =====================================================

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


// =====================================================
// BUILD APPLICATION
// =====================================================

await builder.Build().RunAsync();