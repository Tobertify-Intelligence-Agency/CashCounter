using CashCount.Shared.Models;

namespace CashCount.Shared.Services;

public interface IStorageService
{
    Task<List<SavedCount>> GetSavedCountsAsync();
    Task<SavedCount?> GetByIdAsync(string id);
    Task SaveCountAsync(SavedCount count);
    Task DeleteCountAsync(string id);
    Task ClearAllAsync();

    Task<List<TravelCollection>> GetSavedTripsAsync();
    Task<TravelCollection?> GetTripByIdAsync(string id);
    Task SaveTripAsync(TravelCollection trip);
    Task DeleteTripAsync(string id);
    Task ClearAllTripsAsync();
}
