using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using CashCount.Web;
using CashCount.Shared.Services;
using CashCount.Shared.Services.Auth;
using CashCount.Shared.Services.Billing;
using CashCount.Web.Services.Auth;
using CashCount.Web.Services.Billing;
using CashCount.Web.Services;
using CashCount.Shared.Services.Localization;
using CashCount.Shared.Services.Sync;
using CashCount.Web.Services.Sync;

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

// Register cloud sync.
//
// The order matters conceptually, not technically: LocalStorageService is the
// real store, SyncingStorageService is the decorator every component talks to.
// Components keep injecting IStorageService and notice nothing.
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<ICloudSyncStore, WebCloudSyncStore>();
builder.Services.AddScoped<ISyncCoordinator, SyncCoordinator>();

// Register other services
builder.Services.AddScoped<IStorageService, SyncingStorageService>();
builder.Services.AddScoped<IFileExportService, WebFileExportService>();
builder.Services.AddScoped<ISavedCountPdfService, SavedCountPdfService>();
builder.Services.AddScoped<SavedCountExportService>();
builder.Services.AddScoped<SavedCountReceiptService>();
builder.Services.AddScoped<IPremiumService, PremiumService>();
builder.Services.AddScoped<IAppTextService, AppTextService>();

await builder.Build().RunAsync();
