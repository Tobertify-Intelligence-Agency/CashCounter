using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using CashCount.Web;
using CashCount.Shared.Services;
using CashCount.Shared.Services.Auth;
using CashCount.Shared.Services.Billing;
using CashCount.Web.Services.Auth;
using CashCount.Web.Services.Billing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register authorization services
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CashCountAuthStateProvider>();

// Register auth services
builder.Services.AddScoped<IAuthService, WebAuthService>();
builder.Services.AddScoped<IUserSyncService, WebUserSyncService>();

// Register billing services
builder.Services.AddScoped<IBillingService, WebBillingService>();

// Register other services
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddScoped<IPremiumService, PremiumService>();

await builder.Build().RunAsync();
