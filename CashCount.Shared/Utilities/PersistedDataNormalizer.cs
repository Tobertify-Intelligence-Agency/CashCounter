using CashCount.Shared.Models;

namespace CashCount.Shared.Utilities;

public static class PersistedDataNormalizer
{
    public static List<SavedCount> NormalizeCounts(IEnumerable<SavedCount>? counts)
    {
        return counts?
            .Where(count => count is not null)
            .Select(NormalizeCount)
            .OrderByDescending(count => count.SavedAt)
            .ToList() ?? new List<SavedCount>();
    }

    public static SavedCount NormalizeCount(SavedCount? count)
    {
        var normalized = count ?? new SavedCount();
        normalized.Id = NormalizeId(normalized.Id);
        normalized.Name = NormalizeName(normalized.Name, 80);
        normalized.CurrencyCode = NormalizeName(normalized.CurrencyCode, 8);
        normalized.CurrencySymbol = NormalizeCurrencySymbol(normalized.CurrencySymbol);
        normalized.TotalAmount = Math.Max(0, normalized.TotalAmount);
        normalized.BanknotesTotal = Math.Max(0, normalized.BanknotesTotal);
        normalized.CoinsTotal = Math.Max(0, normalized.CoinsTotal);
        normalized.Denominations = normalized.Denominations?
            .Where(denomination => denomination is not null && denomination.Quantity > 0 && denomination.Value > 0)
            .Select(denomination => new DenominationCount
            {
                Value = denomination.Value,
                DisplayName = NormalizeName(denomination.DisplayName, 40),
                Quantity = Math.Max(0, denomination.Quantity),
                IsCoin = denomination.IsCoin
            })
            .ToList() ?? new List<DenominationCount>();

        return normalized;
    }

    public static List<TravelCollection> NormalizeTrips(IEnumerable<TravelCollection>? trips)
    {
        return trips?
            .Where(trip => trip is not null)
            .Select(NormalizeTrip)
            .OrderByDescending(trip => trip.CreatedAt)
            .ToList() ?? new List<TravelCollection>();
    }

    public static TravelCollection NormalizeTrip(TravelCollection? trip)
    {
        var normalized = trip ?? new TravelCollection();
        normalized.Id = NormalizeId(normalized.Id);
        normalized.Name = NormalizeName(normalized.Name, 80);
        normalized.CurrencySymbol = NormalizeCurrencySymbol(normalized.CurrencySymbol);
        normalized.Entries = normalized.Entries?
            .Where(entry => entry is not null)
            .Select(NormalizeEntry)
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Description) && entry.Amount > 0)
            .OrderByDescending(entry => entry.Date)
            .ToList() ?? new List<TravelCostEntry>();

        return normalized;
    }

    public static TravelCostEntry NormalizeEntry(TravelCostEntry? entry)
    {
        var normalized = entry ?? new TravelCostEntry();
        normalized.Id = NormalizeId(normalized.Id);
        normalized.Description = NormalizeName(normalized.Description, 120);
        normalized.Category = NormalizeName(normalized.Category, 60);
        normalized.Amount = Math.Max(0, normalized.Amount);

        return normalized;
    }

    public static string NormalizeName(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength].TrimEnd();
    }

    private static string NormalizeCurrencySymbol(string? symbol)
    {
        var normalized = NormalizeName(symbol, 8);
        return string.IsNullOrWhiteSpace(normalized) ? "€" : normalized;
    }

    private static string NormalizeId(string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id;
    }
}
