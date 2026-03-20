using System.Text.Json;
using CashCount.Shared.Models;
using CashCount.Shared.Utilities;
using Microsoft.JSInterop;

namespace CashCount.Shared.Services;

public class LocalStorageService : IStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "cashcount_saved_counts";

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<List<SavedCount>> GetSavedCountsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json))
                return new List<SavedCount>();

            var counts = JsonSerializer.Deserialize<List<SavedCount>>(json);
            return PersistedDataNormalizer.NormalizeCounts(counts);
        }
        catch
        {
            return new List<SavedCount>();
        }
    }

    public async Task<SavedCount?> GetByIdAsync(string id)
    {
        var counts = await GetSavedCountsAsync();
        return counts.FirstOrDefault(c => c.Id == id);
    }

    public async Task SaveCountAsync(SavedCount count)
    {
        var counts = await GetSavedCountsAsync();
        var normalizedCount = PersistedDataNormalizer.NormalizeCount(count);

        // Check if updating existing or adding new
        var existingIndex = counts.FindIndex(c => c.Id == normalizedCount.Id);
        if (existingIndex >= 0)
        {
            counts[existingIndex] = normalizedCount;
        }
        else
        {
            counts.Insert(0, normalizedCount); // Add new at the beginning
        }

        var json = JsonSerializer.Serialize(PersistedDataNormalizer.NormalizeCounts(counts));
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task DeleteCountAsync(string id)
    {
        var counts = await GetSavedCountsAsync();
        counts.RemoveAll(c => c.Id == id);

        var json = JsonSerializer.Serialize(PersistedDataNormalizer.NormalizeCounts(counts));
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task ClearAllAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    private const string TripsStorageKey = "cashcount_saved_trips";

    public async Task<List<TravelCollection>> GetSavedTripsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TripsStorageKey);
            if (string.IsNullOrEmpty(json))
                return new List<TravelCollection>();

            var trips = JsonSerializer.Deserialize<List<TravelCollection>>(json);
            return PersistedDataNormalizer.NormalizeTrips(trips);
        }
        catch
        {
            return new List<TravelCollection>();
        }
    }

    public async Task<TravelCollection?> GetTripByIdAsync(string id)
    {
        var trips = await GetSavedTripsAsync();
        return trips.FirstOrDefault(t => t.Id == id);
    }

    public async Task SaveTripAsync(TravelCollection trip)
    {
        var trips = await GetSavedTripsAsync();
        var normalizedTrip = PersistedDataNormalizer.NormalizeTrip(trip);

        var existingIndex = trips.FindIndex(t => t.Id == normalizedTrip.Id);
        if (existingIndex >= 0)
            trips[existingIndex] = normalizedTrip;
        else
            trips.Insert(0, normalizedTrip);

        var json = JsonSerializer.Serialize(PersistedDataNormalizer.NormalizeTrips(trips));
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TripsStorageKey, json);
    }

    public async Task DeleteTripAsync(string id)
    {
        var trips = await GetSavedTripsAsync();
        trips.RemoveAll(t => t.Id == id);

        var json = JsonSerializer.Serialize(PersistedDataNormalizer.NormalizeTrips(trips));
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TripsStorageKey, json);
    }

    public async Task ClearAllTripsAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TripsStorageKey);
    }
}
