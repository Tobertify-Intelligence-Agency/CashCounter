using Microsoft.JSInterop;
using CashCount.Shared.Models;
using CashCount.Shared.Services.Auth;

namespace CashCount.Shared.Services;

/// <summary>
/// Premium service implementation with Firebase sync.
/// - When logged in: checks/updates Firebase Firestore
/// - When not logged in: falls back to localStorage
/// </summary>
public class PremiumService : IPremiumService, IDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IAuthService _authService;
    private readonly IUserSyncService _userSyncService;
    private const string LocalPremiumKey = "cashcount_premium_status";

    private bool? _cachedStatus;
    private string? _cachedUserId;

    public PremiumService(
        IJSRuntime jsRuntime,
        IAuthService authService,
        IUserSyncService userSyncService)
    {
        _jsRuntime = jsRuntime;
        _authService = authService;
        _userSyncService = userSyncService;

        // Listen for auth state changes to reset cache
        _authService.AuthStateChanged += OnAuthStateChanged;
    }

    private void OnAuthStateChanged(object? sender, UserProfile? user)
    {
        // Reset cache when auth state changes
        if (_cachedUserId != user?.UserId)
        {
            _cachedStatus = null;
            _cachedUserId = user?.UserId;
        }
    }

    public async Task<bool> IsPremiumAsync()
    {
        // Return cached value if available
        if (_cachedStatus.HasValue)
            return _cachedStatus.Value;

        // Try to get from Firebase if logged in
        var user = await _authService.GetCurrentUserAsync();
        if (user != null)
        {
            var profile = await _userSyncService.GetUserProfileAsync(user.UserId);
            if (profile != null)
            {
                // Check if premium has expired
                if (profile.PremiumExpiryDate.HasValue &&
                    profile.PremiumExpiryDate.Value < DateTime.UtcNow)
                {
                    _cachedStatus = false;
                    return false;
                }

                _cachedStatus = profile.IsPremium;
                _cachedUserId = user.UserId;

                // Also update localStorage as backup
                await UpdateLocalStorageAsync(profile.IsPremium);

                return profile.IsPremium;
            }
        }

        // Fall back to localStorage
        return await GetFromLocalStorageAsync();
    }

    public async Task<bool> IsFeatureEnabledAsync(PremiumFeature feature)
    {
        var isPremium = await IsPremiumAsync();

        // Define which features require premium
        return feature switch
        {
            PremiumFeature.CurrencySelection => isPremium,
            PremiumFeature.SaveCounts => isPremium,
            PremiumFeature.LoadCounts => isPremium,
            _ => true // Unknown features are enabled by default
        };
    }

    public async Task SetPremiumStatusAsync(bool isPremium)
    {
        // Update localStorage first (offline fallback)
        await UpdateLocalStorageAsync(isPremium);

        // If logged in, persist to Firebase — only update cache after success
        var user = await _authService.GetCurrentUserAsync();
        if (user != null)
        {
            var expiryDate = isPremium ? DateTime.UtcNow.AddYears(1) : (DateTime?)null;

            await _userSyncService.UpdatePremiumStatusAsync(
                user.UserId,
                isPremium,
                expiryDate);

            var profile = await _userSyncService.GetUserProfileAsync(user.UserId);
            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    PhotoUrl = user.PhotoUrl,
                    AuthProvider = user.AuthProvider,
                    IsPremium = isPremium,
                    PremiumExpiryDate = expiryDate,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                await _userSyncService.SaveUserProfileAsync(profile);
            }
        }

        // Update in-memory cache only after all persistence succeeded
        _cachedStatus = isPremium;
    }

    private async Task<bool> GetFromLocalStorageAsync()
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", LocalPremiumKey);
            _cachedStatus = value == "true";
            return _cachedStatus.Value;
        }
        catch
        {
            return false;
        }
    }

    private async Task UpdateLocalStorageAsync(bool isPremium)
    {
        try
        {
            if (isPremium)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", LocalPremiumKey, "true");
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", LocalPremiumKey);
            }
        }
        catch
        {
            // Ignore storage errors
        }
    }

    /// <summary>
    /// Force refresh premium status from Firebase.
    /// Useful after restoring purchases or when sync is needed.
    /// </summary>
    public async Task RefreshPremiumStatusAsync()
    {
        _cachedStatus = null;
        await IsPremiumAsync();
    }

    public void Dispose()
    {
        _authService.AuthStateChanged -= OnAuthStateChanged;
    }
}
