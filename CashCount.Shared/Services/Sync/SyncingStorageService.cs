using System.Text.Json;
using CashCount.Shared.Models;
using Microsoft.Extensions.Logging;

namespace CashCount.Shared.Services.Sync;

/// <summary>
/// The storage service every component talks to once sync is switched on.
///
/// It is a decorator around <see cref="LocalStorageService"/> and does three
/// things the plain local storage cannot:
///
/// <list type="number">
///   <item><description>It stamps every write with <c>UpdatedAt = UtcNow</c>, which is
///   what makes last-write-wins possible at all.</description></item>
///   <item><description>It turns deletions into tombstones instead of removals, so a
///   deletion can travel to the other devices. Reads filter tombstones out again,
///   so no component ever sees one.</description></item>
///   <item><description>It tells the <see cref="ISyncCoordinator"/> that something
///   changed.</description></item>
/// </list>
///
/// Everything stays offline-first: the local write happens first and always
/// succeeds; the cloud is informed afterwards and asynchronously.
/// </summary>
public sealed class SyncingStorageService : IStorageService
{
    /// <summary>
    /// Tombstones are purged by the coordinator after 30 days. This service purges
    /// far later, so that a device which never syncs (offline, signed out) still
    /// does not accumulate tombstones forever, without stealing the coordinator's
    /// chance to purge the cloud copy too.
    /// </summary>
    private static readonly TimeSpan LocalTombstoneLifetime = TimeSpan.FromDays(90);

    /// <summary>Attachment ids whose picture is no longer referenced by any transaction.</summary>
    internal const string OrphanAttachmentsKey = "cashcount_sync_orphan_attachments";

    private readonly LocalStorageService _inner;
    private readonly ISyncCoordinator _coordinator;
    private readonly ILogger<SyncingStorageService> _logger;

    public SyncingStorageService(
        LocalStorageService inner,
        ISyncCoordinator coordinator,
        ILogger<SyncingStorageService> logger)
    {
        _inner = inner;
        _coordinator = coordinator;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // Counts
    // ------------------------------------------------------------------

    public async Task<List<SavedCount>> GetSavedCountsAsync()
    {
        var counts = await LoadCountsAsync();
        return counts.Where(c => !c.IsDeleted).ToList();
    }

    public async Task<SavedCount?> GetByIdAsync(string id)
    {
        var counts = await LoadCountsAsync();
        var match = counts.FirstOrDefault(c => c.Id == id);
        return match is null || match.IsDeleted ? null : match;
    }

    public async Task SaveCountAsync(SavedCount count)
    {
        count.UpdatedAt = DateTime.UtcNow;
        count.IsDeleted = false;

        await _inner.SaveCountAsync(count);
        _coordinator.RequestSync("count saved");
    }

    public async Task DeleteCountAsync(string id)
    {
        var counts = await LoadCountsAsync();
        var match = counts.FirstOrDefault(c => c.Id == id);
        if (match is null)
            return;

        match.IsDeleted = true;
        match.UpdatedAt = DateTime.UtcNow;

        await _inner.ReplaceSavedCountsAsync(counts);
        _coordinator.RequestSync("count deleted");
    }

    public async Task ClearAllAsync()
    {
        var counts = await LoadCountsAsync();
        var now = DateTime.UtcNow;

        foreach (var count in counts.Where(c => !c.IsDeleted))
        {
            count.IsDeleted = true;
            count.UpdatedAt = now;
        }

        await _inner.ReplaceSavedCountsAsync(counts);
        _coordinator.RequestSync("counts cleared");
    }

    /// <summary>Unfiltered counts: normalised, expired tombstones dropped.</summary>
    private async Task<List<SavedCount>> LoadCountsAsync()
    {
        var counts = await _inner.GetSavedCountsAsync();
        var changed = SyncNormalization.NormalizeCounts(counts);

        var kept = DropExpiredTombstones(counts, ref changed);

        if (changed)
            await _inner.ReplaceSavedCountsAsync(kept);

        return kept;
    }

    // ------------------------------------------------------------------
    // Trips
    // ------------------------------------------------------------------

    public async Task<List<TravelCollection>> GetSavedTripsAsync()
    {
        var trips = await LoadTripsAsync();
        return trips.Where(t => !t.IsDeleted).ToList();
    }

    public async Task<TravelCollection?> GetTripByIdAsync(string id)
    {
        var trips = await LoadTripsAsync();
        var match = trips.FirstOrDefault(t => t.Id == id);
        return match is null || match.IsDeleted ? null : match;
    }

    public async Task SaveTripAsync(TravelCollection trip)
    {
        trip.UpdatedAt = DateTime.UtcNow;
        trip.IsDeleted = false;

        await _inner.SaveTripAsync(trip);
        _coordinator.RequestSync("trip saved");
    }

    public async Task DeleteTripAsync(string id)
    {
        var trips = await LoadTripsAsync();
        var match = trips.FirstOrDefault(t => t.Id == id);
        if (match is null)
            return;

        match.IsDeleted = true;
        match.UpdatedAt = DateTime.UtcNow;

        await _inner.ReplaceSavedTripsAsync(trips);
        _coordinator.RequestSync("trip deleted");
    }

    public async Task ClearAllTripsAsync()
    {
        var trips = await LoadTripsAsync();
        var now = DateTime.UtcNow;

        foreach (var trip in trips.Where(t => !t.IsDeleted))
        {
            trip.IsDeleted = true;
            trip.UpdatedAt = now;
        }

        await _inner.ReplaceSavedTripsAsync(trips);
        _coordinator.RequestSync("trips cleared");
    }

    private async Task<List<TravelCollection>> LoadTripsAsync()
    {
        var trips = await _inner.GetSavedTripsAsync();
        var changed = SyncNormalization.NormalizeTrips(trips);

        var kept = DropExpiredTombstones(trips, ref changed);

        if (changed)
            await _inner.ReplaceSavedTripsAsync(kept);

        return kept;
    }

    // ------------------------------------------------------------------
    // Account ledger
    // ------------------------------------------------------------------

    public async Task<AccountLedger> GetAccountLedgerAsync()
    {
        var stored = await LoadLedgerAsync();

        // The component gets a ledger without tombstones. The lists are new, the
        // records themselves are shared — harmless, because the stored ledger is
        // deserialised fresh on every call.
        var visible = new AccountLedger
        {
            Id = stored.Id,
            Name = stored.Name,
            CurrencySymbol = stored.CurrencySymbol,
            CreatedAt = stored.CreatedAt,
            UpdatedAt = stored.UpdatedAt,
            Accounts = stored.Accounts.Where(a => !a.IsDeleted).ToList(),
            Transactions = stored.Transactions.Where(t => !t.IsDeleted).ToList()
        };

        return visible;
    }

    /// <summary>
    /// The ledger is the one place where the component hands us a complete object
    /// graph instead of calling a delete method: rows are removed by taking them
    /// out of the list. So we diff the incoming ledger against the stored one,
    /// stamp what actually changed, and re-add the missing rows as tombstones —
    /// into a separate instance, never into the list the component is still
    /// showing, or the deleted rows would pop straight back into the UI.
    /// </summary>
    public async Task SaveAccountLedgerAsync(AccountLedger ledger)
    {
        var stored = await LoadLedgerAsync();
        var now = DateTime.UtcNow;

        var toStore = new AccountLedger
        {
            Id = string.IsNullOrEmpty(ledger.Id) ? stored.Id : ledger.Id,
            Name = ledger.Name,
            CurrencySymbol = ledger.CurrencySymbol,
            CreatedAt = ledger.CreatedAt == default ? stored.CreatedAt : ledger.CreatedAt,
            UpdatedAt = stored.UpdatedAt
        };

        // Only the ledger's own fields bump the ledger stamp; a new transaction is
        // the transaction's business, not the ledger's.
        if (!string.Equals(stored.Name, ledger.Name, StringComparison.Ordinal) ||
            !string.Equals(stored.CurrencySymbol, ledger.CurrencySymbol, StringComparison.Ordinal) ||
            toStore.UpdatedAt == default)
        {
            toStore.UpdatedAt = now;
        }

        var storedAccounts = stored.Accounts.ToDictionary(a => a.Id, StringComparer.Ordinal);
        var storedTransactions = stored.Transactions.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var orphanedAttachments = new List<string>();

        foreach (var account in ledger.Accounts)
        {
            if (storedAccounts.TryGetValue(account.Id, out var previous) && !previous.IsDeleted)
                account.UpdatedAt = SameContent(previous, account) ? previous.UpdatedAt : now;
            else
                account.UpdatedAt = now;

            account.IsDeleted = false;
            toStore.Accounts.Add(account);
        }

        foreach (var transaction in ledger.Transactions)
        {
            AccountTransaction? previous = storedTransactions.GetValueOrDefault(transaction.Id);

            // A tombstone is not a previous version: an id that comes back is a new
            // record as far as the diff is concerned.
            if (previous is { IsDeleted: true })
                previous = null;

            ReconcileAttachment(transaction, previous, orphanedAttachments);

            transaction.UpdatedAt = previous is not null && SameContent(previous, transaction)
                ? previous.UpdatedAt
                : now;

            transaction.IsDeleted = false;
            toStore.Transactions.Add(transaction);
        }

        // Whatever the component dropped from its lists is a deletion.
        var keptAccountIds = new HashSet<string>(ledger.Accounts.Select(a => a.Id), StringComparer.Ordinal);
        foreach (var previous in stored.Accounts)
        {
            if (keptAccountIds.Contains(previous.Id))
                continue;

            toStore.Accounts.Add(Tombstone(previous, now));
        }

        var keptTransactionIds = new HashSet<string>(ledger.Transactions.Select(t => t.Id), StringComparer.Ordinal);
        foreach (var previous in stored.Transactions)
        {
            if (keptTransactionIds.Contains(previous.Id))
                continue;

            if (!previous.IsDeleted && !string.IsNullOrEmpty(previous.AttachmentId))
                orphanedAttachments.Add(previous.AttachmentId!);

            toStore.Transactions.Add(Tombstone(previous, now));
        }

        await _inner.WriteAccountLedgerAsync(toStore);
        await RememberOrphanedAttachmentsAsync(orphanedAttachments);

        // Keep the component's own instance in step with what we stored, so a
        // second save in the same session does not diff against stale values.
        ledger.UpdatedAt = toStore.UpdatedAt;

        _coordinator.RequestSync("ledger saved");
    }

    public async Task ClearAccountLedgerAsync()
    {
        var stored = await LoadLedgerAsync();
        var now = DateTime.UtcNow;
        var orphaned = new List<string>();

        var toStore = new AccountLedger
        {
            Id = stored.Id,
            Name = stored.Name,
            CurrencySymbol = stored.CurrencySymbol,
            CreatedAt = stored.CreatedAt,
            UpdatedAt = now
        };

        foreach (var account in stored.Accounts)
            toStore.Accounts.Add(account.IsDeleted ? account : Tombstone(account, now));

        foreach (var transaction in stored.Transactions)
        {
            if (!transaction.IsDeleted && !string.IsNullOrEmpty(transaction.AttachmentId))
                orphaned.Add(transaction.AttachmentId!);

            toStore.Transactions.Add(transaction.IsDeleted ? transaction : Tombstone(transaction, now));
        }

        await _inner.WriteAccountLedgerAsync(toStore);
        await RememberOrphanedAttachmentsAsync(orphaned);

        _coordinator.RequestSync("ledger cleared");
    }

    /// <summary>Unfiltered ledger: normalised, expired tombstones dropped.</summary>
    private async Task<AccountLedger> LoadLedgerAsync()
    {
        var ledger = await _inner.GetAccountLedgerAsync();
        var changed = SyncNormalization.NormalizeLedger(ledger);

        var cutoff = DateTime.UtcNow - LocalTombstoneLifetime;

        var accounts = ledger.Accounts.Where(a => !IsExpiredTombstone(a, cutoff)).ToList();
        if (accounts.Count != ledger.Accounts.Count)
        {
            ledger.Accounts = accounts;
            changed = true;
        }

        var transactions = ledger.Transactions.Where(t => !IsExpiredTombstone(t, cutoff)).ToList();
        if (transactions.Count != ledger.Transactions.Count)
        {
            ledger.Transactions = transactions;
            changed = true;
        }

        if (changed)
            await _inner.WriteAccountLedgerAsync(ledger);

        return ledger;
    }

    // ------------------------------------------------------------------
    // Attachments
    // ------------------------------------------------------------------

    /// <summary>
    /// Gives a new picture an id and remembers the id of the picture it replaced,
    /// so the cloud copy of the old one can be deleted on the next run.
    /// </summary>
    private static void ReconcileAttachment(
        AccountTransaction transaction,
        AccountTransaction? previous,
        List<string> orphaned)
    {
        var previousId = previous?.AttachmentId;
        var samePicture = previous is not null &&
                          string.Equals(previous.AttachmentDataUrl, transaction.AttachmentDataUrl, StringComparison.Ordinal);

        if (!transaction.HasAttachment)
        {
            if (!string.IsNullOrEmpty(previousId))
                orphaned.Add(previousId!);

            transaction.AttachmentId = null;
            return;
        }

        if (samePicture && !string.IsNullOrEmpty(previousId))
        {
            // Unchanged picture keeps its id even if the caller lost it.
            transaction.AttachmentId = previousId;
            return;
        }

        var replacesAnotherPicture = previous is not null && !samePicture;

        if (replacesAnotherPicture && !string.IsNullOrEmpty(previousId))
            orphaned.Add(previousId!);

        // A picture is immutable: a different picture is a different id. An id that
        // is already set and refers to this very picture is kept.
        if (replacesAnotherPicture || string.IsNullOrEmpty(transaction.AttachmentId))
            transaction.AttachmentId = Guid.NewGuid().ToString("N");
    }

    private async Task RememberOrphanedAttachmentsAsync(List<string> ids)
    {
        if (ids.Count == 0)
            return;

        var pending = await ReadOrphanedAttachmentsAsync(_inner);
        var before = pending.Count;

        foreach (var id in ids)
            pending.Add(id);

        if (pending.Count == before)
            return;

        await _inner.SetRawAsync(OrphanAttachmentsKey, JsonSerializer.Serialize(pending));
    }

    /// <summary>Attachment ids waiting to be removed from the cloud.</summary>
    internal static async Task<HashSet<string>> ReadOrphanedAttachmentsAsync(LocalStorageService storage)
    {
        var raw = await storage.GetRawAsync(OrphanAttachmentsKey);
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>(StringComparer.Ordinal);

        try
        {
            var ids = JsonSerializer.Deserialize<List<string>>(raw);
            return ids is null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(ids, StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private List<T> DropExpiredTombstones<T>(List<T> records, ref bool changed) where T : ISyncable
    {
        var cutoff = DateTime.UtcNow - LocalTombstoneLifetime;
        var kept = records.Where(r => !IsExpiredTombstone(r, cutoff)).ToList();

        if (kept.Count != records.Count)
        {
            changed = true;
            _logger.LogInformation("Purged {Count} expired tombstones.", records.Count - kept.Count);
        }

        return kept;
    }

    private static bool IsExpiredTombstone(ISyncable record, DateTime cutoff)
        => record.IsDeleted && record.UpdatedAt != default && record.UpdatedAt < cutoff;

    private static TrackedAccount Tombstone(TrackedAccount source, DateTime now)
    {
        if (source.IsDeleted)
            return source;

        return new TrackedAccount
        {
            Id = source.Id,
            Name = source.Name,
            Kind = source.Kind,
            OpeningBalance = source.OpeningBalance,
            Notes = source.Notes,
            IsDeleted = true,
            UpdatedAt = now
        };
    }

    private static AccountTransaction Tombstone(AccountTransaction source, DateTime now)
    {
        if (source.IsDeleted)
            return source;

        return new AccountTransaction
        {
            Id = source.Id,
            AccountId = source.AccountId,
            Description = source.Description,
            Amount = source.Amount,
            Direction = source.Direction,
            Category = source.Category,
            Date = source.Date,
            Notes = source.Notes,
            // A tombstone drops the picture: deleted is deleted, and it keeps the
            // stored ledger from growing without bound.
            AttachmentDataUrl = null,
            AttachmentFileName = null,
            AttachmentId = null,
            IsDeleted = true,
            UpdatedAt = now
        };
    }

    private static bool SameContent(TrackedAccount a, TrackedAccount b)
        => string.Equals(a.Name, b.Name, StringComparison.Ordinal)
           && a.Kind == b.Kind
           && a.OpeningBalance == b.OpeningBalance
           && string.Equals(a.Notes, b.Notes, StringComparison.Ordinal);

    private static bool SameContent(AccountTransaction a, AccountTransaction b)
        => string.Equals(a.AccountId, b.AccountId, StringComparison.Ordinal)
           && string.Equals(a.Description, b.Description, StringComparison.Ordinal)
           && a.Amount == b.Amount
           && a.Direction == b.Direction
           && string.Equals(a.Category, b.Category, StringComparison.Ordinal)
           && a.Date == b.Date
           && string.Equals(a.Notes, b.Notes, StringComparison.Ordinal)
           && string.Equals(a.AttachmentDataUrl, b.AttachmentDataUrl, StringComparison.Ordinal)
           && string.Equals(a.AttachmentFileName, b.AttachmentFileName, StringComparison.Ordinal);
}
