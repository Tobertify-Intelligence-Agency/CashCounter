using System.Text.Json;
using Microsoft.JSInterop;
using CashCount.Shared.Models;
using CashCount.Shared.Services.Auth;

namespace CashCount.Web.Services.Auth;

/// <summary>
/// Web implementation of IUserSyncService using Firebase Firestore JS SDK.
/// </summary>
public class WebUserSyncService : IUserSyncService
{
    private readonly IJSRuntime _jsRuntime;

    public WebUserSyncService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>(
                "firebaseFirestore.getUserProfile", userId);

            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<UserProfile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetUserProfile error: {ex.Message}");
            return null;
        }
    }

    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        try
        {
            var json = JsonSerializer.Serialize(profile);
            await _jsRuntime.InvokeVoidAsync(
                "firebaseFirestore.saveUserProfile", profile.UserId, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveUserProfile error: {ex.Message}");
        }
    }

    public async Task UpdatePremiumStatusAsync(string userId, bool isPremium, DateTime? expiryDate)
    {
        try
        {
            var expiryMs = expiryDate?.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
            await _jsRuntime.InvokeVoidAsync(
                "firebaseFirestore.updatePremiumStatus", userId, isPremium, expiryMs);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdatePremiumStatus error: {ex.Message}");
        }
    }

    public async Task SyncSavedCountsAsync(string userId, List<SavedCount> counts)
    {
        try
        {
            var json = JsonSerializer.Serialize(counts);
            await _jsRuntime.InvokeVoidAsync(
                "firebaseFirestore.syncSavedCounts", userId, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncSavedCounts error: {ex.Message}");
        }
    }

    public async Task<List<SavedCount>> GetSyncedCountsAsync(string userId)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>(
                "firebaseFirestore.getSyncedCounts", userId);

            if (string.IsNullOrEmpty(json))
                return new List<SavedCount>();

            return JsonSerializer.Deserialize<List<SavedCount>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SavedCount>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetSyncedCounts error: {ex.Message}");
            return new List<SavedCount>();
        }
    }

    public async Task DeleteUserDataAsync(string userId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "firebaseFirestore.deleteUserData", userId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteUserData error: {ex.Message}");
        }
    }
}
