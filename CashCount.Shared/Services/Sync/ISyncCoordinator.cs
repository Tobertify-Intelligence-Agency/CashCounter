namespace CashCount.Shared.Services.Sync;

/// <summary>
/// What the sync layer is currently doing. Rendered in the account settings, so
/// the user can tell "synced" from "silently doing nothing".
/// </summary>
public enum SyncState
{
    /// <summary>No cloud backend on this platform (Windows/macOS desktop build).</summary>
    Unavailable,

    /// <summary>Nobody is signed in — data stays on this device.</summary>
    SignedOut,

    /// <summary>Signed in, but cloud sync is a premium feature.</summary>
    PremiumRequired,

    /// <summary>Ready; nothing to do right now.</summary>
    Idle,

    /// <summary>A sync run is in progress.</summary>
    Syncing,

    /// <summary>The last run failed; see <see cref="ISyncCoordinator.LastError"/>.</summary>
    Error
}

/// <summary>
/// Drives synchronisation. Everything that changes data calls
/// <see cref="RequestSync"/>; the coordinator debounces those calls so a burst of
/// edits results in one cloud round trip.
/// </summary>
public interface ISyncCoordinator
{
    SyncState State { get; }

    /// <summary>UTC time of the last successful run, or null.</summary>
    DateTime? LastSyncedAt { get; }

    /// <summary>Message of the last failure, or null.</summary>
    string? LastError { get; }

    /// <summary>Raised whenever <see cref="State"/> or <see cref="LastSyncedAt"/> changes.</summary>
    event Action? Changed;

    /// <summary>
    /// Raised after a run that actually brought something down from another device.
    /// Pages that hold data in a field subscribe to this and reload; without it a
    /// sync would be invisible until the user navigates away and back, which looks
    /// exactly like sync not working.
    /// </summary>
    event Action? DataPulled;

    /// <summary>Schedules a debounced sync run. Never throws, never blocks.</summary>
    void RequestSync(string reason);

    /// <summary>Runs a sync immediately and waits for it (manual "sync now" button).</summary>
    Task SyncNowAsync();

    /// <summary>
    /// Loads the timestamp of the last successful run from local storage, so the
    /// settings page does not report "never synced" after every restart. Cheap and
    /// idempotent; call it when the status is first rendered.
    /// </summary>
    Task EnsureLastSyncLoadedAsync();
}
