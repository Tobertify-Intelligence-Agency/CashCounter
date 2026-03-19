using Android.App;
using Android.Content;
using Android.Content.PM;
using Plugin.Firebase.Auth.Google;

namespace CashCount;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        // Handle Google Sign-In result
        FirebaseAuthGoogleImplementation.HandleActivityResultAsync(requestCode, resultCode, data);
    }
}
