using Microsoft.JSInterop;
using CashCount.Shared.Models;
using CashCount.Shared.Services.Auth;

namespace CashCount.Web.Services.Auth;

/// <summary>
/// Web implementation of IAuthService using Firebase JS SDK via JSInterop.
/// </summary>
public class WebAuthService : IAuthService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private DotNetObjectReference<WebAuthService>? _dotNetRef;
    private UserProfile? _currentUser;
    private bool _initialized;

    public event EventHandler<UserProfile?>? AuthStateChanged;

    public WebAuthService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("firebaseAuth.initialize", _dotNetRef);
            _initialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Firebase JS initialization failed: {ex.Message}");
        }
    }

    [JSInvokable]
    public void OnAuthStateChanged(UserProfileJs? userJs)
    {
        _currentUser = userJs != null ? MapFromJs(userJs) : null;
        AuthStateChanged?.Invoke(this, _currentUser);
    }

    public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
    {
        await EnsureInitializedAsync();

        try
        {
            var userJs = await _jsRuntime.InvokeAsync<UserProfileJs>(
                "firebaseAuth.signInWithEmail", email, password);

            _currentUser = MapFromJs(userJs);
            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser);
        }
        catch (JSException ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex.Message));
        }
    }

    public async Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName)
    {
        await EnsureInitializedAsync();

        try
        {
            var userJs = await _jsRuntime.InvokeAsync<UserProfileJs>(
                "firebaseAuth.signUpWithEmail", email, password, displayName);

            _currentUser = MapFromJs(userJs);
            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser);
        }
        catch (JSException ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex.Message));
        }
    }

    public async Task<AuthResult> SignInWithGoogleAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var userJs = await _jsRuntime.InvokeAsync<UserProfileJs>(
                "firebaseAuth.signInWithGoogle");

            _currentUser = MapFromJs(userJs);
            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser);
        }
        catch (JSException ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex.Message));
        }
    }

    public async Task<AuthResult> SignInWithAppleAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var userJs = await _jsRuntime.InvokeAsync<UserProfileJs>(
                "firebaseAuth.signInWithApple");

            _currentUser = MapFromJs(userJs);
            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser);
        }
        catch (JSException ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex.Message));
        }
    }

    public async Task<AuthResult> SignInWithMicrosoftAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            var userJs = await _jsRuntime.InvokeAsync<UserProfileJs>(
                "firebaseAuth.signInWithMicrosoft");

            _currentUser = MapFromJs(userJs);
            AuthStateChanged?.Invoke(this, _currentUser);
            return AuthResult.Succeeded(_currentUser);
        }
        catch (JSException ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex.Message));
        }
    }

    public async Task SignOutAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            await _jsRuntime.InvokeVoidAsync("firebaseAuth.signOut");
            _currentUser = null;
            AuthStateChanged?.Invoke(this, null);
        }
        catch (JSException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sign out error: {ex.Message}");
        }
    }

    public async Task<UserProfile?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return _currentUser;

        await EnsureInitializedAsync();

        try
        {
            var userJs = await _jsRuntime.InvokeAsync<UserProfileJs?>(
                "firebaseAuth.getCurrentUser");

            _currentUser = userJs != null ? MapFromJs(userJs) : null;
            return _currentUser;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsSignedInAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }

    public async Task<AuthResult> SendPasswordResetEmailAsync(string email)
    {
        await EnsureInitializedAsync();

        try
        {
            await _jsRuntime.InvokeVoidAsync("firebaseAuth.sendPasswordResetEmail", email);
            return new AuthResult { Success = true };
        }
        catch (JSException ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex.Message));
        }
    }

    public async Task<AuthResult> UpdateDisplayNameAsync(string displayName)
    {
        await EnsureInitializedAsync();

        try
        {
            await _jsRuntime.InvokeVoidAsync("firebaseAuth.updateDisplayName", displayName);

            if (_currentUser != null)
            {
                _currentUser.DisplayName = displayName;
            }

            return AuthResult.Succeeded(_currentUser!);
        }
        catch (JSException ex)
        {
            return AuthResult.Failed(GetFriendlyErrorMessage(ex.Message));
        }
    }

    private UserProfile MapFromJs(UserProfileJs userJs)
    {
        return new UserProfile
        {
            UserId = userJs.UserId,
            Email = userJs.Email ?? string.Empty,
            DisplayName = userJs.DisplayName ?? userJs.Email?.Split('@')[0] ?? "User",
            PhotoUrl = userJs.PhotoUrl,
            AuthProvider = userJs.AuthProvider,
            LastLoginAt = DateTime.UtcNow
        };
    }

    private string GetFriendlyErrorMessage(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        if (lowerMessage.Contains("invalid-email"))
            return "Invalid email address format.";
        if (lowerMessage.Contains("user-disabled"))
            return "This account has been disabled.";
        if (lowerMessage.Contains("user-not-found"))
            return "No account found with this email.";
        if (lowerMessage.Contains("wrong-password"))
            return "Incorrect password.";
        if (lowerMessage.Contains("email-already-in-use"))
            return "An account with this email already exists.";
        if (lowerMessage.Contains("weak-password"))
            return "Password is too weak. Please use at least 6 characters.";
        if (lowerMessage.Contains("network"))
            return "Network error. Please check your connection.";
        if (lowerMessage.Contains("popup-closed") || lowerMessage.Contains("cancelled"))
            return "Sign in was cancelled.";

        return message;
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
    }
}

/// <summary>
/// JavaScript interop model for user profile.
/// </summary>
public class UserProfileJs
{
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? AuthProvider { get; set; }
}
