using System.Text.Json;
using CashCount.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace CashCount.Shared.Services;

public class LocalStorageService : IStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;
    private const string StorageKey = "cashcount_saved_counts";
    private const string TripsStorageKey = "cashcount_saved_trips";
    private const string AccountLedgerStorageKey = "cashcount_account_ledger";

    public LocalStorageService(IJSRuntime jsRuntime, ILogger<LocalStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<List<SavedCount>> GetSavedCountsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json))
                return new List<SavedCount>();

            return JsonSerializer.Deserialize<List<SavedCount>>(json) ?? new List<SavedCount>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved counts from localStorage.");
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

        var existingIndex = counts.FindIndex(c => c.Id == count.Id);
        if (existingIndex >= 0)
            counts[existingIndex] = count;
        else
            counts.Insert(0, count);

        var json = JsonSerializer.Serialize(counts);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task DeleteCountAsync(string id)
    {
        var counts = await GetSavedCountsAsync();
        counts.RemoveAll(c => c.Id == id);

        var json = JsonSerializer.Serialize(counts);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task ClearAllAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    public async Task<List<TravelCollection>> GetSavedTripsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TripsStorageKey);
            if (string.IsNullOrEmpty(json))
                return new List<TravelCollection>();

            return JsonSerializer.Deserialize<List<TravelCollection>>(json) ?? new List<TravelCollection>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved trips from localStorage.");
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

        var existingIndex = trips.FindIndex(t => t.Id == trip.Id);
        if (existingIndex >= 0)
            trips[existingIndex] = trip;
        else
            trips.Insert(0, trip);

        var json = JsonSerializer.Serialize(trips);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TripsStorageKey, json);
    }

    public async Task DeleteTripAsync(string id)
    {
        var trips = await GetSavedTripsAsync();
        trips.RemoveAll(t => t.Id == id);

        var json = JsonSerializer.Serialize(trips);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TripsStorageKey, json);
    }

    public async Task ClearAllTripsAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TripsStorageKey);
    }

    public async Task<AccountLedger> GetAccountLedgerAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", AccountLedgerStorageKey);
            if (string.IsNullOrWhiteSpace(json))
                return new AccountLedger();

            return JsonSerializer.Deserialize<AccountLedger>(json) ?? new AccountLedger();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load account ledger from localStorage.");
            return new AccountLedger();
        }
    }

    public async Task SaveAccountLedgerAsync(AccountLedger ledger)
    {
        ledger.Touch();
        var json = JsonSerializer.Serialize(ledger);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccountLedgerStorageKey, json);
    }

    public async Task ClearAccountLedgerAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccountLedgerStorageKey);
    }
}
