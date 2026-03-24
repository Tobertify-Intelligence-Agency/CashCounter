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

        StatusBarBackground.HeightRequest = 0;
        StatusBarBackground.IsVisible = false;
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
