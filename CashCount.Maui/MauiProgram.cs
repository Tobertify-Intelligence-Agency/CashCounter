using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using CashCount.Shared.Services;
using CashCount.Shared.Services.Auth;
using CashCount.Shared.Services.Billing;
using CashCount.Shared.Services.Localization;
using CashCount.Maui.Services.Auth;
using CashCount.Maui.Services.Billing;
using CashCount.Maui.Services;
using CashCount.Maui.Services.Sync;
using CashCount.Shared.Services.Sync;
#if ANDROID || IOS
using Plugin.Firebase.Auth;
#endif

namespace CashCount;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			})
			.RegisterFirebaseServices();

		builder.Services.AddMauiBlazorWebView();

		// Register authorization services
		builder.Services.AddAuthorizationCore();
		builder.Services.AddScoped<AuthenticationStateProvider, CashCountAuthStateProvider>();

		// Register auth services
		builder.Services.AddScoped<IAuthService, MauiAuthService>();
		builder.Services.AddScoped<IUserSyncService, MauiUserSyncService>();
        
        // Register billing services
        builder.Services.AddScoped<IBillingService, MauiBillingService>();

		// Register cloud sync.
		//
		// LocalStorageService stays the real store; SyncingStorageService is the
		// decorator every component talks to through IStorageService, so no
		// component had to change.
		builder.Services.AddScoped<LocalStorageService>();
		builder.Services.AddScoped<ICloudSyncStore, MauiCloudSyncStore>();
		builder.Services.AddScoped<ISyncCoordinator, SyncCoordinator>();

		// Register other services
		builder.Services.AddScoped<IStorageService, SyncingStorageService>();
		builder.Services.AddScoped<IFileExportService, MauiFileExportService>();
		builder.Services.AddScoped<ISavedCountPdfService, SavedCountPdfService>();
		builder.Services.AddScoped<SavedCountExportService>();
		builder.Services.AddScoped<SavedCountReceiptService>();
		builder.Services.AddScoped<IPremiumService, PremiumService>();
		builder.Services.AddScoped<IAppTextService, AppTextService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
