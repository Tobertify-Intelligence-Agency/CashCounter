using CashCount.Shared.Models;
using CashCount.Shared.Services.Auth;

#if ANDROID
using Plugin.Firebase.Auth;
using Plugin.Firebase.Auth.Google;
#elif IOS
using Plugin.Firebase.Auth;
#endif

namespace CashCount.Maui.Services.Auth;

/// <summary>
/// MAUI implementation of IAuthService using Plugin.Firebase.Auth.
/// Supports Android and iOS. Windows/macOS fall back to stub implementation.
/// </summary>
public class MauiAuthService : IAuthService
{
    private UserProfile? _currentUser;
#if ANDROID || IOS
    private IDisposable? _authStateListener;
#endif

    public event EventHandler<UserProfile?>? AuthStateChanged;

#if ANDROID || IOS
    public MauiAuthService()
    {
        try
        {
            // Listen for auth state changes using AddAuthStateListener which returns IDisposable
            _authStateListener = CrossFirebaseAuth.Current.AddAuthStateListener(OnAuthStateChanged);

            // Initialize current user if already signed in
            var user = CrossFirebaseAuth.Current.CurrentUser;
            if (user != null)
            {
                _currentUser = MapToUserProfile(user);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Firebase Auth initialization: {ex.Message}");
        }
    }

    private void OnAuthStateChanged(IFirebaseAuth auth)
    {
        try
        {
            var user = auth.CurrentUser;
            _currentUser = user != null ? MapToUserProfile(user) : null;
            AuthStateChanged?.Invoke(this, _currentUser);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Auth state changed error: {ex.Message}");
        }
    }

    public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
    {
        try
        {
            // API returns IFirebaseUser directly
            var user = await CrossFirebaseAuth.Current.SignInWithEmailAndPasswordAsync(email, password);
            _currentUser = MapToUserProfile(user);
            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser!);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex));
        }
    }

    public async Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName)
    {
        try
        {
            // Use CreateUserAsync for creating new users
            var user = await CrossFirebaseAuth.Current.CreateUserAsync(email, password);

            // Update display name using UpdateProfileAsync(displayName, photoUrl)
            if (!string.IsNullOrEmpty(displayName) && user != null)
            {
                await user.UpdateProfileAsync(displayName, null);
            }

            _currentUser = MapToUserProfile(user);
            if (_currentUser != null)
            {
                _currentUser.DisplayName = displayName;
            }
            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser!);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex));
        }
    }

    public async Task<AuthResult> SignInWithGoogleAsync()
    {
#if ANDROID
        try
        {
            // Use Plugin.Firebase.Auth.Google to trigger Google Sign-In flow
            // This will show the Google account picker and authenticate
            var credential = await CrossFirebaseAuthGoogle.Current.SignInWithGoogleAsync();      

            _currentUser = MapToUserProfile(credential);
            if (_currentUser != null)
            {
                _currentUser.AuthProvider = "google.com";
            }
            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser!);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Google Sign-In error: {ex.Message}");
            return AuthResult.Failed(GetFriendlyErrorMessage(ex));
        }
#else
        // iOS Google Sign-In requires Plugin.Firebase.Auth.Google package for iOS
        // and additional configuration (GoogleService-Info.plist, URL schemes)
        return await Task.FromResult(AuthResult.Failed("Google sign-in is not yet configured for iOS. Please use email/password sign-in."));
#endif
    }

    public Task<AuthResult> SignInWithAppleAsync()
    {
        // Apple Sign-In requires Plugin.Firebase.Auth.Apple package and iOS setup
        return Task.FromResult(AuthResult.Failed("Apple sign-in is not yet configured. Please use email/password sign-in."));
    }

    public Task<AuthResult> SignInWithMicrosoftAsync()
    {
        // Microsoft Sign-In requires custom OAuth implementation
        return Task.FromResult(AuthResult.Failed("Microsoft sign-in is not yet configured. Please use email/password sign-in."));
    }

    public async Task SignOutAsync()
    {
        try
        {
            await CrossFirebaseAuth.Current.SignOutAsync();
            _currentUser = null;
            AuthStateChanged?.Invoke(this, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sign out error: {ex.Message}");
        }
    }

    public Task<UserProfile?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return Task.FromResult<UserProfile?>(_currentUser);

        try
        {
            var firebaseUser = CrossFirebaseAuth.Current.CurrentUser;
            _currentUser = firebaseUser != null ? MapToUserProfile(firebaseUser) : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetCurrentUser error: {ex.Message}");
        }

        return Task.FromResult(_currentUser);
    }

    public async Task<bool> IsSignedInAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }

    public async Task<AuthResult> SendPasswordResetEmailAsync(string email)
    {
        try
        {
            await CrossFirebaseAuth.Current.SendPasswordResetEmailAsync(email);
            return new AuthResult { Success = true };
        }
        catch (Exception ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex));
        }
    }

    public async Task<AuthResult> UpdateDisplayNameAsync(string displayName)
    {
        try
        {
            var user = CrossFirebaseAuth.Current.CurrentUser;
            if (user == null)
                return AuthResult.Failed("Not signed in");

            // Use UpdateProfileAsync(displayName, photoUrl) - pass null to keep existing photo
            await user.UpdateProfileAsync(displayName, null);

            if (_currentUser != null)
            {
                _currentUser.DisplayName = displayName;
            }
            return AuthResult.Succeeded(_currentUser!);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex));
        }
    }

    private UserProfile? MapToUserProfile(IFirebaseUser? user)
    {
        if (user == null) return null;

        // Use ProviderId directly - ProviderData doesn't exist in this API version
        string authProvider = user.ProviderId ?? "firebase";

        return new UserProfile
        {
            UserId = user.Uid,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName ?? user.Email?.Split('@')[0] ?? "User",
            PhotoUrl = user.PhotoUrl,
            AuthProvider = authProvider,
            LastLoginAt = DateTime.UtcNow
        };
    }

    private string GetFriendlyErrorMessage(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();

        if (message.Contains("invalid-email") || message.Contains("invalid email"))
            return "Invalid email address format.";
        if (message.Contains("user-disabled"))
            return "This account has been disabled.";
        if (message.Contains("user-not-found") || message.Contains("no user"))
            return "No account found with this email.";
        if (message.Contains("wrong-password") || message.Contains("invalid password"))
            return "Incorrect password.";
        if (message.Contains("email-already-in-use") || message.Contains("already exists"))
            return "An account with this email already exists.";
        if (message.Contains("weak-password"))
            return "Password is too weak. Please use at least 6 characters.";
        if (message.Contains("network") || message.Contains("connection"))
            return "Network error. Please check your connection.";
        if (message.Contains("cancelled") || message.Contains("canceled"))
            return "Sign in was cancelled.";
        if (message.Contains("credential"))
            return "Invalid credentials. Please try again.";

        return ex.Message;
    }

#else
    // Windows/macOS - Firebase REST API (Identity Toolkit)
    private const string FirebaseApiKey = "AIzaSyDSHEpC5yxstLLAUxwSoC2Z-qdTqRdkCHo";
    private const string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + FirebaseApiKey;
    private const string SignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + FirebaseApiKey;
    private const string UpdateProfileUrl = "https://identitytoolkit.googleapis.com/v1/accounts:update?key=" + FirebaseApiKey;
    private const string SendPasswordResetUrl = "https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key=" + FirebaseApiKey;
    private const string GetUserInfoUrl = "https://identitytoolkit.googleapis.com/v1/accounts:lookup?key=" + FirebaseApiKey;
    private const string RefreshTokenUrl = "https://securetoken.googleapis.com/v1/token?key=" + FirebaseApiKey;

    private static readonly HttpClient _httpClient = new();

    public MauiAuthService()
    {
        _ = TryRestoreSessionAsync();
    }

    private async Task TryRestoreSessionAsync()
    {
        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync("firebase_refresh_token");
            if (string.IsNullOrEmpty(refreshToken)) return;

            var payload = System.Text.Json.JsonSerializer.Serialize(new { grant_type = "refresh_token", refresh_token = refreshToken });
            var response = await _httpClient.PostAsync(RefreshTokenUrl, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode) return;

            var content = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenResponse>(content);
            if (result?.id_token == null) return;

            if (!string.IsNullOrEmpty(result.refresh_token))
                await SecureStorage.Default.SetAsync("firebase_refresh_token", result.refresh_token);

            _currentUser = await GetUserInfoFromTokenAsync(result.id_token);
            if (_currentUser != null)
                AuthStateChanged?.Invoke(this, _currentUser);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Session restore error: {ex.Message}");
        }
    }

    private async Task<UserProfile?> GetUserInfoFromTokenAsync(string idToken)
    {
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { idToken });
            var response = await _httpClient.PostAsync(GetUserInfoUrl, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            var result = System.Text.Json.JsonSerializer.Deserialize<GetUserInfoResponse>(content);
            var user = result?.users?.FirstOrDefault();
            if (user == null) return null;

            return new UserProfile
            {
                UserId = user.localId ?? string.Empty,
                Email = user.email ?? string.Empty,
                DisplayName = user.displayName ?? user.email?.Split('@')[0] ?? "User",
                PhotoUrl = user.photoUrl,
                AuthProvider = "password",
                LastLoginAt = DateTime.UtcNow
            };
        }
        catch { return null; }
    }

    public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
    {
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { email, password, returnSecureToken = true });
            var response = await _httpClient.PostAsync(SignInUrl, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = System.Text.Json.JsonSerializer.Deserialize<FirebaseErrorResponse>(content);
                return AuthResult.Failed(GetFriendlyErrorMessage(err?.error?.message ?? "Sign in failed"));
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<FirebaseAuthResponse>(content);
            if (result == null) return AuthResult.Failed("Invalid response from server");

            _currentUser = new UserProfile
            {
                UserId = result.localId ?? string.Empty,
                Email = result.email ?? email,
                DisplayName = result.displayName ?? email.Split('@')[0],
                PhotoUrl = result.photoUrl,
                AuthProvider = "password",
                LastLoginAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(result.refreshToken))
                await SecureStorage.Default.SetAsync("firebase_refresh_token", result.refreshToken);

            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed(ex.Message);
        }
    }

    public async Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName)
    {
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { email, password, returnSecureToken = true });
            var response = await _httpClient.PostAsync(SignUpUrl, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = System.Text.Json.JsonSerializer.Deserialize<FirebaseErrorResponse>(content);
                return AuthResult.Failed(GetFriendlyErrorMessage(err?.error?.message ?? "Sign up failed"));
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<FirebaseAuthResponse>(content);
            if (result == null) return AuthResult.Failed("Invalid response from server");

            // Update display name if provided
            if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(result.idToken))
            {
                var updatePayload = System.Text.Json.JsonSerializer.Serialize(new { idToken = result.idToken, displayName, returnSecureToken = false });
                await _httpClient.PostAsync(UpdateProfileUrl, new StringContent(updatePayload, System.Text.Encoding.UTF8, "application/json"));
            }

            _currentUser = new UserProfile
            {
                UserId = result.localId ?? string.Empty,
                Email = result.email ?? email,
                DisplayName = !string.IsNullOrEmpty(displayName) ? displayName : email.Split('@')[0],
                AuthProvider = "password",
                LastLoginAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(result.refreshToken))
                await SecureStorage.Default.SetAsync("firebase_refresh_token", result.refreshToken);

            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed(ex.Message);
        }
    }

    public Task<AuthResult> SignInWithGoogleAsync()
        => Task.FromResult(AuthResult.Failed("Google sign-in is not yet supported on Windows. Please use email/password sign-in."));

    public Task<AuthResult> SignInWithAppleAsync()
        => Task.FromResult(AuthResult.Failed("Apple sign-in is not supported on Windows. Please use email/password sign-in."));

    public Task<AuthResult> SignInWithMicrosoftAsync()
        => Task.FromResult(AuthResult.Failed("Microsoft sign-in is not yet supported on Windows. Please use email/password sign-in."));

    public Task SignOutAsync()
    {
        _currentUser = null;
        SecureStorage.Default.Remove("firebase_refresh_token");
        AuthStateChanged?.Invoke(this, null);
        return Task.CompletedTask;
    }

    public Task<UserProfile?> GetCurrentUserAsync()
        => Task.FromResult(_currentUser);

    public Task<bool> IsSignedInAsync()
        => Task.FromResult(_currentUser != null);

    public async Task<AuthResult> SendPasswordResetEmailAsync(string email)
    {
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { requestType = "PASSWORD_RESET", email });
            var response = await _httpClient.PostAsync(SendPasswordResetUrl, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = System.Text.Json.JsonSerializer.Deserialize<FirebaseErrorResponse>(content);
                return AuthResult.Failed(GetFriendlyErrorMessage(err?.error?.message ?? "Failed to send reset email"));
            }

            return new AuthResult { Success = true };
        }
        catch (Exception ex)
        {
            return AuthResult.Failed(ex.Message);
        }
    }

    public async Task<AuthResult> UpdateDisplayNameAsync(string displayName)
    {
        if (_currentUser == null)
            return AuthResult.Failed("Not signed in");

        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync("firebase_refresh_token");
            if (string.IsNullOrEmpty(refreshToken))
                return AuthResult.Failed("Session expired. Please sign in again.");

            var refreshPayload = System.Text.Json.JsonSerializer.Serialize(new { grant_type = "refresh_token", refresh_token = refreshToken });
            var refreshResponse = await _httpClient.PostAsync(RefreshTokenUrl, new StringContent(refreshPayload, System.Text.Encoding.UTF8, "application/json"));
            if (!refreshResponse.IsSuccessStatusCode)
                return AuthResult.Failed("Session expired. Please sign in again.");

            var refreshContent = await refreshResponse.Content.ReadAsStringAsync();
            var refreshResult = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenResponse>(refreshContent);
            if (refreshResult?.id_token == null)
                return AuthResult.Failed("Session expired. Please sign in again.");

            var payload = System.Text.Json.JsonSerializer.Serialize(new { idToken = refreshResult.id_token, displayName, returnSecureToken = false });
            var response = await _httpClient.PostAsync(UpdateProfileUrl, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = System.Text.Json.JsonSerializer.Deserialize<FirebaseErrorResponse>(content);
                return AuthResult.Failed(GetFriendlyErrorMessage(err?.error?.message ?? "Update failed"));
            }

            _currentUser.DisplayName = displayName;
            return AuthResult.Succeeded(_currentUser);
        }
        catch (Exception ex)
        {
            return AuthResult.Failed(ex.Message);
        }
    }

    private static string GetFriendlyErrorMessage(string message) => message switch
    {
        "EMAIL_NOT_FOUND" => "No account found with this email.",
        "INVALID_PASSWORD" => "Incorrect password.",
        "USER_DISABLED" => "This account has been disabled.",
        "EMAIL_EXISTS" => "An account with this email already exists.",
        "INVALID_EMAIL" => "Invalid email address format.",
        "INVALID_LOGIN_CREDENTIALS" => "Invalid email or password.",
        "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many failed attempts. Please try again later.",
        _ when message.StartsWith("WEAK_PASSWORD") => "Password must be at least 6 characters.",
        _ => message
    };

    // REST API response models
    private class FirebaseAuthResponse
    {
        public string? idToken { get; set; }
        public string? email { get; set; }
        public string? refreshToken { get; set; }
        public string? localId { get; set; }
        public string? displayName { get; set; }
        public string? photoUrl { get; set; }
    }

    private class RefreshTokenResponse
    {
        public string? refresh_token { get; set; }
        public string? id_token { get; set; }
    }

    private class GetUserInfoResponse
    {
        public List<FirebaseUserInfo>? users { get; set; }
    }

    private class FirebaseUserInfo
    {
        public string? localId { get; set; }
        public string? email { get; set; }
        public string? displayName { get; set; }
        public string? photoUrl { get; set; }
    }

    private class FirebaseErrorResponse
    {
        public FirebaseError? error { get; set; }
    }

    private class FirebaseError
    {
        public string? message { get; set; }
    }
#endif
}
