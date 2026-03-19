namespace CashCount.Shared.Services;

/// <summary>
/// Service to manage premium feature access.
/// </summary>
public interface IPremiumService
{
    /// <summary>
    /// Gets whether the user has premium access.
    /// </summary>
    Task<bool> IsPremiumAsync();

    /// <summary>
    /// Gets whether a specific feature is enabled.
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(PremiumFeature feature);

    /// <summary>
    /// Sets the premium status (for testing or after purchase verification).
    /// </summary>
    Task SetPremiumStatusAsync(bool isPremium);
}

/// <summary>
/// Premium features that can be individually checked.
/// </summary>
public enum PremiumFeature
{
    /// <summary>
    /// Ability to switch between different currencies.
    /// </summary>
    CurrencySelection,

    /// <summary>
    /// Ability to save cash counts for later.
    /// </summary>
    SaveCounts,

    /// <summary>
    /// Ability to load previously saved counts.
    /// </summary>
    LoadCounts
}
