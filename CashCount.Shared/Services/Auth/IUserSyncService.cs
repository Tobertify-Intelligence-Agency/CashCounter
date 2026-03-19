using CashCount.Shared.Models;

namespace CashCount.Shared.Services.Auth;

/// <summary>
/// Service for syncing user data with Firestore.
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// Get user profile from Firestore.
    /// </summary>
    Task<UserProfile?> GetUserProfileAsync(string userId);

    /// <summary>
    /// Save or update user profile in Firestore.
    /// </summary>
    Task SaveUserProfileAsync(UserProfile profile);

    /// <summary>
    /// Update premium status in Firestore.
    /// </summary>
    Task UpdatePremiumStatusAsync(string userId, bool isPremium, DateTime? expiryDate);

    /// <summary>
    /// Sync saved counts to Firestore.
    /// </summary>
    Task SyncSavedCountsAsync(string userId, List<SavedCount> counts);

    /// <summary>
    /// Get saved counts from Firestore.
    /// </summary>
    Task<List<SavedCount>> GetSyncedCountsAsync(string userId);

    /// <summary>
    /// Delete user data from Firestore.
    /// </summary>
    Task DeleteUserDataAsync(string userId);
}
