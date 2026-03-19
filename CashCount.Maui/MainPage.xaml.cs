namespace CashCount;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
		SetSafeAreaPadding();
	}

	private void SetSafeAreaPadding()
	{
#if ANDROID
		// Get the status bar height on Android
		var statusBarHeight = 0;
		var resourceId = Android.Content.Res.Resources.System?.GetIdentifier("status_bar_height", "dimen", "android");
		if (resourceId.HasValue && resourceId.Value > 0)
		{
			statusBarHeight = Android.Content.Res.Resources.System!.GetDimensionPixelSize(resourceId.Value);
			// Convert pixels to device-independent units
			var density = DeviceDisplay.MainDisplayInfo.Density;
			var statusBarHeightDp = statusBarHeight / density;
			StatusBarBackground.HeightRequest = statusBarHeightDp;
		}
#elif IOS || MACCATALYST
		// iOS handles safe area automatically, but set a minimum
		StatusBarBackground.HeightRequest = 0;
		StatusBarBackground.IsVisible = false;
#else
		// Windows/Desktop - no status bar needed
		StatusBarBackground.HeightRequest = 0;
		StatusBarBackground.IsVisible = false;
#endif
	}
}
