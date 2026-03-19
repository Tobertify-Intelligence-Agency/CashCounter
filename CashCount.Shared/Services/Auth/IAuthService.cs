using CashCount.Shared.Models;

namespace CashCount.Shared.Services.Auth;

/// <summary>
/// Service for user authentication (Firebase Auth).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Sign in with email and password.
    /// </summary>
    Task<AuthResult> SignInWithEmailAsync(string email, string password);

    /// <summary>
    /// Create a new account with email and password.
    /// </summary>
    Task<AuthResult> SignUpWithEmailAsync(string email, string password, string displayName);

    /// <summary>
    /// Sign in with Google OAuth.
    /// </summary>
    Task<AuthResult> SignInWithGoogleAsync();

    /// <summary>
    /// Sign in with Apple OAuth.
    /// </summary>
    Task<AuthResult> SignInWithAppleAsync();

    /// <summary>
    /// Sign in with Microsoft OAuth.
    /// </summary>
    Task<AuthResult> SignInWithMicrosoftAsync();

    /// <summary>
    /// Sign out the current user.
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Get the currently signed-in user, or null if not signed in.
    /// </summary>
    Task<UserProfile?> GetCurrentUserAsync();

    /// <summary>
    /// Check if a user is currently signed in.
    /// </summary>
    Task<bool> IsSignedInAsync();

    /// <summary>
    /// Send a password reset email.
    /// </summary>
    Task<AuthResult> SendPasswordResetEmailAsync(string email);

    /// <summary>
    /// Update the current user's display name.
    /// </summary>
    Task<AuthResult> UpdateDisplayNameAsync(string displayName);

    /// <summary>
    /// Event fired when authentication state changes.
    /// </summary>
    event EventHandler<UserProfile?> AuthStateChanged;
}
