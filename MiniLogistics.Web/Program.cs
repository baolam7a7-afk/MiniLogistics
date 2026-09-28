
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MiniLogistics.Web;
using MiniLogistics.Web.Services.Auth;
using MiniLogistics.Web.Services.Product;
using MiniLogistics.Web.Services.Cart;
using MiniLogistics.Web.Services.Address;
using MiniLogistics.Web.Services.Order;
using MiniLogistics.Web.Services.User;
using MiniLogistics.Web.Services.Review;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP Client
builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5136/")
    });

// Authentication
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenStorageService>();
builder.Services.AddScoped<AuthStateService>();

// Product, Cart, Address, Order
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<AddressService>();
builder.Services.AddScoped<OrderService>();

// User Profile
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ReviewService>();

await builder.Build().RunAsync();