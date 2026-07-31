using System.Globalization;
using System.Text.Json;
using CashCount.Shared.Models;
using CashCount.Shared.Services.Auth;
using Microsoft.Extensions.Logging;

namespace CashCount.Shared.Services.Sync;

/// <summary>
/// Runs the actual synchronisation: pull the cloud collections, merge them with
/// the local data, write the result back on both sides.
///
/// One run is one pass over counts, trips and the ledger. Runs never overlap (a
/// semaphore serialises them) and a failure is turned into a visible error state
/// rather than being swallowed — the previous sync attempt in this app failed
/// silently, which is precisely why nobody noticed it never ran.
/// </summary>
public sealed class SyncCoordinator : ISyncCoordinator, IDisposable
{
    /// <summary>
    /// How long a burst of edits is collected before one cloud round trip. Typing
    /// a transaction fires several saves; there is no point in three uploads.
    /// </summary>
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Firestore refuses documents larger than 1 MiB. Pictures are already capped
    /// at 600 KB before base64, so this is a guard rail, not a normal case.
    /// </summary>
    private const int MaxPayloadChars = 900_000;

    private const string LastSyncKey = "cashcount_sync_last_run";
    private const string UploadedAttachmentsKey = "cashcount_sync_uploaded_attachments";

    private readonly IAuthService _auth;
    private readonly IPremiumService _premium;
    private readonly ICloudSyncStore _store;
    private readonly LocalStorageService _storage;
    private readonly ILogger<SyncCoordinator> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _debounce;
    private bool _lastSyncLoaded;

    public SyncCoordinator(
        IAuthService auth,
        IPremiumService premium,
        ICloudSyncStore store,
        LocalStorageService storage,
        ILogger<SyncCoordinator> logger)
    {
        _auth = auth;
        _premium = premium;
        _store = store;
        _storage = storage;
        _logger = logger;

        State = store.IsAvailable ? SyncState.Idle : SyncState.Unavailable;

        _auth.AuthStateChanged += OnAuthStateChanged;
    }

    public SyncState State { get; private set; }

    public DateTime? LastSyncedAt { get; private set; }

    public string? LastError { get; private set; }

    public event Action? Changed;

    public event Action? DataPulled;

    public void RequestSync(string reason)
    {
        if (!_store.IsAvailable)
            return;

        var cts = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _debounce, cts);

        try
        {
            previous?.Cancel();
            previous?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Raced with its own completion — nothing to cancel.
        }

        _ = DebounceThenSyncAsync(reason, cts.Token);
    }

    public Task SyncNowAsync() => RunAsync("manual");

    private async Task DebounceThenSyncAsync(string reason, CancellationToken token)
    {
        try
        {
            await Task.Delay(DebounceDelay, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await RunAsync(reason);
    }

    private void OnAuthStateChanged(object? sender, UserProfile? user)
    {
        if (user is null)
        {
            SetState(_store.IsAvailable ? SyncState.SignedOut : SyncState.Unavailable);
            return;
        }

        // Signing in is the one moment where a full pull really matters.
        RequestSync("signed in");
    }

    // ------------------------------------------------------------------
    // One run
    // ------------------------------------------------------------------

    private async Task RunAsync(string reason)
    {
        try
        {
            // Runs never overlap: a manual "sync now" during a debounced run waits
            // for it instead of racing it.
            await _gate.WaitAsync();
        }
        catch (ObjectDisposedException)
        {
            // The scope was torn down while a debounced run was still pending.
            return;
        }

        try
        {
            if (!_store.IsAvailable)
            {
                SetState(SyncState.Unavailable);
                return;
            }

            var user = await _auth.GetCurrentUserAsync();
            if (user is null || string.IsNullOrEmpty(user.UserId))
            {
                SetState(SyncState.SignedOut);
                return;
            }

            if (!await _premium.IsFeatureEnabledAsync(PremiumFeature.CloudSync))
            {
                SetState(SyncState.PremiumRequired);
                return;
            }

            SetState(SyncState.Syncing);
            _logger.LogInformation("Sync started ({Reason}).", reason);

            var now = DateTime.UtcNow;
            var pulled = await SyncCountsAsync(user.UserId, now);
            pulled |= await SyncTripsAsync(user.UserId, now);
            pulled |= await SyncLedgerAsync(user.UserId, now);

            LastSyncedAt = now;
            LastError = null;
            await _storage.SetRawAsync(LastSyncKey, now.ToString("O", CultureInfo.InvariantCulture));

            SetState(SyncState.Idle);
            _logger.LogInformation("Sync finished ({Reason}).", reason);

            // Only now, with the state already consistent: the pages listening to
            // this reload their data, and a reload during the run would read
            // half-merged lists.
            if (pulled)
            {
                _logger.LogInformation("Sync brought down changes; notifying the UI.");
                DataPulled?.Invoke();
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            SetState(SyncState.Error);
            _logger.LogError(ex, "Sync failed ({Reason}).", reason);
        }
        finally
        {
            try
            {
                _gate.Release();
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }

    // ------------------------------------------------------------------
    // Counts and trips
    // ------------------------------------------------------------------

    /// <summary>Returns true when the run changed the local data.</summary>
    private async Task<bool> SyncCountsAsync(string userId, DateTime now)
    {
        var local = await _storage.GetSavedCountsAsync();
        SyncNormalization.NormalizeCounts(local);

        var remote = await _store.GetAllAsync(userId, SyncCollections.Counts);
        var outcome = SyncMerge.Merge(local, remote);

        var merged = SyncMerge.PurgeExpiredTombstones(outcome.Merged, now, out var purgedIds);
        var dropped = new HashSet<string>(purgedIds, StringComparer.Ordinal);

        // Re-read: the user may have saved a count while we were on the network.
        var current = await _storage.GetSavedCountsAsync();
        SyncNormalization.NormalizeCounts(current);

        var final = SyncMerge.MergeRecords(current, merged, dropped);
        final.Sort((a, b) => b.SavedAt.CompareTo(a.SavedAt));

        await _storage.ReplaceSavedCountsAsync(final);
        await PushAsync(userId, SyncCollections.Counts, outcome.ToPush, purgedIds);

        return outcome.LocalChanged || purgedIds.Count > 0;
    }

    /// <summary>Returns true when the run changed the local data.</summary>
    private async Task<bool> SyncTripsAsync(string userId, DateTime now)
    {
        var local = await _storage.GetSavedTripsAsync();
        SyncNormalization.NormalizeTrips(local);

        var remote = await _store.GetAllAsync(userId, SyncCollections.Trips);
        var outcome = SyncMerge.Merge(local, remote);

        var merged = SyncMerge.PurgeExpiredTombstones(outcome.Merged, now, out var purgedIds);
        var dropped = new HashSet<string>(purgedIds, StringComparer.Ordinal);

        var current = await _storage.GetSavedTripsAsync();
        SyncNormalization.NormalizeTrips(current);

        var final = SyncMerge.MergeRecords(current, merged, dropped);
        final.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));

        await _storage.ReplaceSavedTripsAsync(final);
        await PushAsync(userId, SyncCollections.Trips, outcome.ToPush, purgedIds);

        return outcome.LocalChanged || purgedIds.Count > 0;
    }

    // ------------------------------------------------------------------
    // Ledger
    // ------------------------------------------------------------------

    /// <summary>Returns true when the run changed the local data.</summary>
    private async Task<bool> SyncLedgerAsync(string userId, DateTime now)
    {
        var ledger = await _storage.GetAccountLedgerAsync();
        SyncNormalization.NormalizeLedger(ledger);

        var metaChanged = await SyncLedgerMetaAsync(userId, ledger);

        // Accounts
        var remoteAccounts = await _store.GetAllAsync(userId, SyncCollections.Accounts);
        var accountOutcome = SyncMerge.Merge(ledger.Accounts, remoteAccounts);
        var accounts = SyncMerge.PurgeExpiredTombstones(accountOutcome.Merged, now, out var purgedAccounts);

        // Transactions
        var remoteTransactions = await _store.GetAllAsync(userId, SyncCollections.Transactions);
        var transactionOutcome = SyncMerge.Merge(ledger.Transactions, remoteTransactions);

        var stranded = new List<string>();
        CarryPicturesOverRemoteWins(transactionOutcome.ReplacedByRemote, stranded);
        StripPicturesFromRemoteTombstones(transactionOutcome.TombstonedByRemote, stranded);

        var transactions = SyncMerge.PurgeExpiredTombstones(transactionOutcome.Merged, now, out var purgedTransactions);

        var uploaded = await ReadIdSetAsync(UploadedAttachmentsKey);
        var downloaded = await DownloadAttachmentsAsync(userId, transactions, uploaded);

        await WriteLedgerAsync(ledger, accounts, transactions, purgedAccounts, purgedTransactions);

        await PushAsync(userId, SyncCollections.Accounts, accountOutcome.ToPush, purgedAccounts);
        await PushTransactionsAsync(userId, transactionOutcome.ToPush, purgedTransactions);

        await UploadAttachmentsAsync(userId, transactions, uploaded);
        await RemoveAttachmentsAsync(userId, stranded, uploaded);
        await RemoveOrphanedAttachmentsAsync(userId, uploaded);

        await WriteIdSetAsync(UploadedAttachmentsKey, uploaded);

        return metaChanged
               || accountOutcome.LocalChanged
               || transactionOutcome.LocalChanged
               || downloaded
               || purgedAccounts.Count > 0
               || purgedTransactions.Count > 0;
    }

    /// <summary>
    /// A transaction travels through the cloud without its picture — the picture
    /// has its own document. So when the cloud version of a transaction wins, it
    /// arrives with an empty picture, and taking it at face value would delete a
    /// picture this device already has. As long as the attachment id is unchanged
    /// it is still the same picture, so it is carried over. A changed id means the
    /// other device really replaced the picture; the old one is then stranded and
    /// gets deleted from the cloud.
    /// </summary>
    private static void CarryPicturesOverRemoteWins(
        List<RemoteWin<AccountTransaction>> wins,
        List<string> stranded)
    {
        foreach (var win in wins)
        {
            var previous = win.Previous;
            var current = win.Current;

            var sameAttachment = string.Equals(previous.AttachmentId, current.AttachmentId, StringComparison.Ordinal);

            if (sameAttachment)
            {
                if (string.IsNullOrWhiteSpace(current.AttachmentDataUrl) &&
                    !string.IsNullOrWhiteSpace(previous.AttachmentDataUrl))
                {
                    current.AttachmentDataUrl = previous.AttachmentDataUrl;
                    current.AttachmentFileName ??= previous.AttachmentFileName;
                }

                continue;
            }

            if (!string.IsNullOrEmpty(previous.AttachmentId))
                stranded.Add(previous.AttachmentId!);
        }
    }

    /// <summary>
    /// A deletion that arrived from another device must take the picture with it,
    /// here and in the cloud. Without this the base64 image would sit in local
    /// storage until the tombstone expires, and the cloud copy would never go away
    /// at all, because the device that recorded the orphan was the other one.
    /// </summary>
    private static void StripPicturesFromRemoteTombstones(
        List<AccountTransaction> tombstoned,
        List<string> stranded)
    {
        foreach (var transaction in tombstoned)
        {
            if (!string.IsNullOrEmpty(transaction.AttachmentId))
                stranded.Add(transaction.AttachmentId!);

            transaction.AttachmentDataUrl = null;
            transaction.AttachmentFileName = null;
            transaction.AttachmentId = null;
        }
    }

    /// <summary>
    /// Writes the merged ledger, folding in whatever the user changed while this
    /// run was on the network. The remaining window — between this read and the
    /// write below — contains no awaited network call, so it is as small as it can
    /// get without putting a lock around the components' own saves.
    /// </summary>
    private async Task WriteLedgerAsync(
        AccountLedger synced,
        List<TrackedAccount> accounts,
        List<AccountTransaction> transactions,
        List<string> purgedAccounts,
        List<string> purgedTransactions)
    {
        var current = await _storage.GetAccountLedgerAsync();
        SyncNormalization.NormalizeLedger(current, assignAttachmentIds: false);

        var toStore = new AccountLedger
        {
            Id = synced.Id,
            Name = synced.Name,
            CurrencySymbol = synced.CurrencySymbol,
            CreatedAt = synced.CreatedAt,
            UpdatedAt = synced.UpdatedAt
        };

        // A rename during the run is newer than what we merged, so it wins.
        if (current.UpdatedAt > synced.UpdatedAt)
        {
            toStore.Name = current.Name;
            toStore.CurrencySymbol = current.CurrencySymbol;
            toStore.UpdatedAt = current.UpdatedAt;
        }

        toStore.Accounts = SyncMerge.MergeRecords(
            current.Accounts,
            accounts,
            new HashSet<string>(purgedAccounts, StringComparer.Ordinal));

        toStore.Transactions = SyncMerge.MergeRecords(
            current.Transactions,
            transactions,
            new HashSet<string>(purgedTransactions, StringComparer.Ordinal));

        // MergeRecords keeps the re-read record on a tie, and a tie is exactly the
        // case where this run's own attachment work is missing from it.
        var syncedById = new Dictionary<string, AccountTransaction>(StringComparer.Ordinal);
        foreach (var transaction in transactions)
        {
            if (!string.IsNullOrEmpty(transaction.Id))
                syncedById[transaction.Id] = transaction;
        }

        foreach (var transaction in toStore.Transactions)
        {
            if (!syncedById.TryGetValue(transaction.Id, out var fromRun) || ReferenceEquals(transaction, fromRun))
                continue;

            // This run gave a picture from before sync existed its first id.
            if (string.IsNullOrEmpty(transaction.AttachmentId) &&
                !string.IsNullOrEmpty(fromRun.AttachmentId) &&
                string.Equals(transaction.AttachmentDataUrl, fromRun.AttachmentDataUrl, StringComparison.Ordinal))
            {
                transaction.AttachmentId = fromRun.AttachmentId;
            }

            // This run downloaded the picture from another device.
            if (string.IsNullOrWhiteSpace(transaction.AttachmentDataUrl) &&
                !string.IsNullOrWhiteSpace(fromRun.AttachmentDataUrl) &&
                string.Equals(transaction.AttachmentId, fromRun.AttachmentId, StringComparison.Ordinal))
            {
                transaction.AttachmentDataUrl = fromRun.AttachmentDataUrl;
                transaction.AttachmentFileName ??= fromRun.AttachmentFileName;
            }
        }

        await _storage.WriteAccountLedgerAsync(toStore);
    }

    private async Task<bool> SyncLedgerMetaAsync(string userId, AccountLedger ledger)
    {
        var local = new List<LedgerMeta> { LedgerMeta.From(ledger) };
        var remote = await _store.GetAllAsync(userId, SyncCollections.LedgerMeta);

        var outcome = SyncMerge.Merge(local, remote);

        var winner = outcome.Merged.FirstOrDefault(m => m.Id == SyncCollections.LedgerMetaId);
        winner?.ApplyTo(ledger);

        if (outcome.ToPush.Count > 0)
        {
            await _store.UpsertAsync(
                userId,
                SyncCollections.LedgerMeta,
                outcome.ToPush.Select(SyncMerge.ToDocument).ToList());
        }

        return outcome.LocalChanged;
    }

    /// <summary>
    /// Pushes transactions without their picture. The picture travels in its own
    /// document so that a transaction record stays a few hundred bytes instead of
    /// most of a megabyte.
    /// </summary>
    private async Task PushTransactionsAsync(
        string userId,
        List<AccountTransaction> toPush,
        List<string> purgedIds)
    {
        var purged = new HashSet<string>(purgedIds, StringComparer.Ordinal);

        var documents = toPush
            .Where(t => !purged.Contains(t.Id))
            .Select(t => SyncMerge.ToDocument(WithoutPicture(t)))
            .ToList();

        if (documents.Count > 0)
            await _store.UpsertAsync(userId, SyncCollections.Transactions, documents);

        if (purgedIds.Count > 0)
            await _store.HardDeleteAsync(userId, SyncCollections.Transactions, purgedIds);
    }

    private static AccountTransaction WithoutPicture(AccountTransaction source) => new()
    {
        Id = source.Id,
        AccountId = source.AccountId,
        Description = source.Description,
        Amount = source.Amount,
        Direction = source.Direction,
        Category = source.Category,
        Date = source.Date,
        Notes = source.Notes,
        AttachmentDataUrl = null,
        AttachmentFileName = source.AttachmentFileName,
        AttachmentId = source.AttachmentId,
        UpdatedAt = source.UpdatedAt,
        IsDeleted = source.IsDeleted
    };

    // ------------------------------------------------------------------
    // Attachments
    // ------------------------------------------------------------------

    /// <summary>Returns true when at least one picture was fetched.</summary>
    private async Task<bool> DownloadAttachmentsAsync(
        string userId,
        List<AccountTransaction> transactions,
        HashSet<string> uploaded)
    {
        var any = false;

        foreach (var transaction in transactions)
        {
            if (transaction.IsDeleted || string.IsNullOrEmpty(transaction.AttachmentId))
                continue;

            if (!string.IsNullOrWhiteSpace(transaction.AttachmentDataUrl))
                continue;

            var document = await _store.GetAsync(userId, SyncCollections.Attachments, transaction.AttachmentId!);
            if (document is null || document.IsDeleted || string.IsNullOrWhiteSpace(document.Payload))
            {
                // The other device has not uploaded the picture yet, or it is gone.
                // The transaction itself stays intact; only the picture is missing.
                continue;
            }

            transaction.AttachmentDataUrl = document.Payload;
            uploaded.Add(transaction.AttachmentId!);
            any = true;
        }

        return any;
    }

    private async Task UploadAttachmentsAsync(
        string userId,
        List<AccountTransaction> transactions,
        HashSet<string> uploaded)
    {
        var documents = new List<SyncDocument>();

        foreach (var transaction in transactions)
        {
            if (transaction.IsDeleted || string.IsNullOrEmpty(transaction.AttachmentId))
                continue;

            var data = transaction.AttachmentDataUrl;
            if (string.IsNullOrWhiteSpace(data) || uploaded.Contains(transaction.AttachmentId!))
                continue;

            if (data.Length > MaxPayloadChars)
            {
                _logger.LogWarning(
                    "Attachment {Id} is {Length} characters and too large to sync; it stays on this device.",
                    transaction.AttachmentId,
                    data.Length);
                continue;
            }

            documents.Add(new SyncDocument
            {
                Id = transaction.AttachmentId!,
                UpdatedAt = transaction.UpdatedAt,
                IsDeleted = false,
                Payload = data
            });
        }

        if (documents.Count == 0)
            return;

        // One at a time: a batch of pictures would blow past the write size limit.
        foreach (var document in documents)
        {
            await _store.UpsertAsync(userId, SyncCollections.Attachments, new[] { document });
            uploaded.Add(document.Id);
        }
    }

    /// <summary>
    /// Deletes pictures whose transaction was removed or whose picture was
    /// replaced. The list is filled by <see cref="SyncingStorageService"/> at the
    /// moment the change happens, because that is the only place where the old id
    /// is still known.
    /// </summary>
    private async Task RemoveOrphanedAttachmentsAsync(string userId, HashSet<string> uploaded)
    {
        var orphaned = await SyncingStorageService.ReadOrphanedAttachmentsAsync(_storage);
        if (orphaned.Count == 0)
            return;

        await _store.HardDeleteAsync(userId, SyncCollections.Attachments, orphaned.ToList());

        foreach (var id in orphaned)
            uploaded.Remove(id);

        // Re-read instead of clearing: the user may have replaced another picture
        // while the delete was in flight, and blanking the list would lose that id
        // for good — the cloud copy would then be orphaned forever.
        var pending = await SyncingStorageService.ReadOrphanedAttachmentsAsync(_storage);
        pending.ExceptWith(orphaned);

        await _storage.SetRawAsync(
            SyncingStorageService.OrphanAttachmentsKey,
            JsonSerializer.Serialize(pending.ToList()));
    }

    /// <summary>
    /// Deletes pictures that became unreachable during this run — a remote deletion
    /// or a remote picture swap. Unlike the orphan list these ids never touched
    /// local storage, so they are handled straight away.
    /// </summary>
    private async Task RemoveAttachmentsAsync(string userId, List<string> ids, HashSet<string> uploaded)
    {
        if (ids.Count == 0)
            return;

        var distinct = ids.Distinct(StringComparer.Ordinal).ToList();
        await _store.HardDeleteAsync(userId, SyncCollections.Attachments, distinct);

        foreach (var id in distinct)
            uploaded.Remove(id);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task PushAsync<T>(string userId, string collection, List<T> toPush, List<string> purgedIds)
        where T : class, ISyncable
    {
        var purged = new HashSet<string>(purgedIds, StringComparer.Ordinal);

        var documents = toPush
            .Where(r => !purged.Contains(r.Id))
            .Select(SyncMerge.ToDocument)
            .ToList();

        if (documents.Count > 0)
            await _store.UpsertAsync(userId, collection, documents);

        if (purgedIds.Count > 0)
            await _store.HardDeleteAsync(userId, collection, purgedIds);
    }

    private async Task<HashSet<string>> ReadIdSetAsync(string key)
    {
        var raw = await _storage.GetRawAsync(key);
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

    private Task WriteIdSetAsync(string key, HashSet<string> ids)
        => _storage.SetRawAsync(key, JsonSerializer.Serialize(ids.ToList()));

    /// <summary>
    /// Always raises <see cref="Changed"/>, even when the state is unchanged: the
    /// panel also shows the last run time and the error text, and a second failure
    /// with a different message has to reach the screen.
    /// </summary>
    private void SetState(SyncState state)
    {
        State = state;
        Changed?.Invoke();
    }

    /// <summary>
    /// Restores the timestamp of the last successful run, so the settings page
    /// does not claim "never synced" right after a restart.
    /// </summary>
    public async Task EnsureLastSyncLoadedAsync()
    {
        if (_lastSyncLoaded)
            return;

        _lastSyncLoaded = true;

        var raw = await _storage.GetRawAsync(LastSyncKey);
        if (string.IsNullOrWhiteSpace(raw))
            return;

        if (DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            LastSyncedAt = SyncNormalization.AsUtc(parsed);
            Changed?.Invoke();
        }
    }

    public void Dispose()
    {
        _auth.AuthStateChanged -= OnAuthStateChanged;

        try
        {
            _debounce?.Cancel();
            _debounce?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }

        _gate.Dispose();
    }
}
