namespace CashCount.Shared.Services.Sync;

/// <summary>
/// Platform-specific access to the user's cloud documents.
///
/// Implementations exist for Android/iOS (Plugin.Firebase Firestore) and for the
/// browser (Firestore JS SDK). Windows and macOS have no Firestore binding, so
/// they use a stub whose <see cref="IsAvailable"/> is false — sync is then simply
/// switched off instead of failing repeatedly.
///
/// Implementations must throw on failure rather than swallowing exceptions: the
/// coordinator turns an exception into a visible error state. Silent failure is
/// what made the previous sync attempt undebuggable.
/// </summary>
public interface ICloudSyncStore
{
    /// <summary>False when this platform has no cloud backend at all.</summary>
    bool IsAvailable { get; }

    /// <summary>All documents of one collection, tombstones included.</summary>
    Task<IReadOnlyList<SyncDocument>> GetAllAsync(string userId, string collection);

    /// <summary>A single document, or null when it does not exist.</summary>
    Task<SyncDocument?> GetAsync(string userId, string collection, string id);

    /// <summary>Creates or overwrites the given documents.</summary>
    Task UpsertAsync(string userId, string collection, IReadOnlyList<SyncDocument> documents);

    /// <summary>Removes documents for good (tombstone purge, orphaned pictures).</summary>
    Task HardDeleteAsync(string userId, string collection, IReadOnlyList<string> ids);
}
