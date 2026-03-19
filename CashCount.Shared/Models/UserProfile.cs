namespace CashCount.Shared.Models;

/// <summary>
/// Represents a user profile stored in Firebase.
/// </summary>
public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public bool IsPremium { get; set; }
    public DateTime? PremiumExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public string? AuthProvider { get; set; } // "email", "google.com", "apple.com", "microsoft.com"
}

/// <summary>
/// Result of an authentication operation.
/// </summary>
public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public UserProfile? User { get; set; }

    public static AuthResult Succeeded(UserProfile user) => new() { Success = true, User = user };
    public static AuthResult Failed(string message) => new() { Success = false, ErrorMessage = message };
}
