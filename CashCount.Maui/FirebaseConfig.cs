using Microsoft.Maui.LifecycleEvents;

#if ANDROID
using Firebase;
using Plugin.Firebase.Auth.Google;
#endif

namespace CashCount;

public static class FirebaseConfig
{
    // Web Client ID from google-services.json (client_type: 3)
    // This is required for Google Sign-In to work properly
    private const string GoogleWebClientId = "40575601002-kqevtqij160cg0abmshcm451u4hhhodn.apps.googleusercontent.com";

    public static MauiAppBuilder RegisterFirebaseServices(this MauiAppBuilder builder)
    {
#if ANDROID
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddAndroid(android => android.OnCreate((activity, _) =>
            {
                // Initialize Firebase
                FirebaseApp.InitializeApp(activity);

                // Initialize Google Sign-In with the web client ID
                // Step 3: Plugin.Firebase.Auth.Google initialization
                FirebaseAuthGoogleImplementation.Initialize(GoogleWebClientId);
            }));
        });
#endif

        return builder;
    }
}
