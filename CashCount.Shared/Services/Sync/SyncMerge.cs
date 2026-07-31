using System.Text.Json;
using CashCount.Shared.Models;

namespace CashCount.Shared.Services.Sync;

/// <summary>
/// Result of merging one local list with one cloud collection.
/// </summary>
public sealed class MergeOutcome<T>
{
    public MergeOutcome(
        List<T> merged,
        List<T> toPush,
        bool localChanged,
        List<RemoteWin<T>> replacedByRemote,
        List<T> tombstonedByRemote)
    {
        Merged = merged;
        ToPush = toPush;
        LocalChanged = localChanged;
        ReplacedByRemote = replacedByRemote;
        TombstonedByRemote = tombstonedByRemote;
    }

    /// <summary>The list as it should now be stored locally (tombstones included).</summary>
    public List<T> Merged { get; }

    /// <summary>Records the cloud is missing or has an older version of.</summary>
    public List<T> ToPush { get; }

    /// <summary>True when <see cref="Merged"/> differs from the local input.</summary>
    public bool LocalChanged { get; }

    /// <summary>
    /// Records where the cloud version won over an existing local one. The caller
    /// needs these because the cloud version can legitimately be missing data that
    /// only ever lives locally — a transaction's picture, for instance.
    /// </summary>
    public List<RemoteWin<T>> ReplacedByRemote { get; }

    /// <summary>
    /// Records that another device deleted. They are already marked deleted inside
    /// <see cref="Merged"/>; the caller gets them so it can clean up whatever hangs
    /// off them.
    /// </summary>
    public List<T> TombstonedByRemote { get; }
}

/// <summary>One record whose cloud version replaced the local one.</summary>
public sealed class RemoteWin<T>
{
    public RemoteWin(T previous, T current)
    {
        Previous = previous;
        Current = current;
    }

    /// <summary>The version this device had before the merge.</summary>
    public T Previous { get; }

    /// <summary>The version that came down from the cloud and is now in the list.</summary>
    public T Current { get; }
}

/// <summary>
/// The merge algorithm — pure, side-effect free, and identical on every platform.
///
/// Rules, per record and never per list:
/// <list type="number">
///   <item><description>Known on both sides: the newer <see cref="ISyncable.UpdatedAt"/> wins.</description></item>
///   <item><description>Only local: push it to the cloud.</description></item>
///   <item><description>Only in the cloud: take it — unless it is a tombstone for a
///   record this device never had, which is simply ignored.</description></item>
/// </list>
/// A deletion is an ordinary record whose newest version happens to be a
/// tombstone, so rule 1 also settles "deleted on A, edited on B": the later
/// action wins, and the user is not left with a resurrected record.
/// </summary>
public static class SyncMerge
{
    /// <summary>How long tombstones are kept before they are purged for good.</summary>
    public static readonly TimeSpan TombstoneLifetime = TimeSpan.FromDays(30);

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static SyncDocument ToDocument<T>(T record) where T : ISyncable
    {
        return new SyncDocument
        {
            Id = record.Id,
            UpdatedAt = record.UpdatedAt,
            IsDeleted = record.IsDeleted,
            // A tombstone carries no content: deleted means gone, also in the cloud.
            Payload = record.IsDeleted ? string.Empty : JsonSerializer.Serialize(record, JsonOptions)
        };
    }

    public static T? FromDocument<T>(SyncDocument document) where T : class, ISyncable
    {
        if (document.IsDeleted || string.IsNullOrWhiteSpace(document.Payload))
            return null;

        T? record;
        try
        {
            record = JsonSerializer.Deserialize<T>(document.Payload, JsonOptions);
        }
        catch (JsonException)
        {
            // A single unreadable document must not abort the whole sync.
            return null;
        }

        if (record == null)
            return null;

        record.Id = document.Id;
        record.UpdatedAt = document.UpdatedAt;
        record.IsDeleted = false;
        return record;
    }

    public static MergeOutcome<T> Merge<T>(IReadOnlyList<T> local, IReadOnlyList<SyncDocument> remote)
        where T : class, ISyncable
    {
        var merged = new List<T>(local);
        var push = new List<T>();
        var replaced = new List<RemoteWin<T>>();
        var tombstoned = new List<T>();
        var localChanged = false;

        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < merged.Count; i++)
        {
            var id = merged[i].Id;
            if (!string.IsNullOrEmpty(id))
                index[id] = i;
        }

        var remoteIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var document in remote)
        {
            if (string.IsNullOrEmpty(document.Id))
                continue;

            remoteIds.Add(document.Id);

            if (index.TryGetValue(document.Id, out var position))
            {
                var localRecord = merged[position];
                var comparison = document.UpdatedAt.CompareTo(localRecord.UpdatedAt);

                if (comparison > 0)
                {
                    if (document.IsDeleted)
                    {
                        // Keep the local content but mark it deleted, so the record
                        // disappears from the UI and the tombstone can still travel on.
                        localRecord.IsDeleted = true;
                        localRecord.UpdatedAt = document.UpdatedAt;
                        tombstoned.Add(localRecord);
                        localChanged = true;
                    }
                    else
                    {
                        var decoded = FromDocument<T>(document);
                        if (decoded != null)
                        {
                            merged[position] = decoded;
                            replaced.Add(new RemoteWin<T>(localRecord, decoded));
                            localChanged = true;
                        }
                    }
                }
                else if (comparison < 0)
                {
                    push.Add(localRecord);
                }
            }
            else if (!document.IsDeleted)
            {
                var decoded = FromDocument<T>(document);
                if (decoded != null)
                {
                    merged.Add(decoded);
                    localChanged = true;
                }
            }
        }

        foreach (var localRecord in local)
        {
            if (!string.IsNullOrEmpty(localRecord.Id) && !remoteIds.Contains(localRecord.Id))
                push.Add(localRecord);
        }

        return new MergeOutcome<T>(merged, push, localChanged, replaced, tombstoned);
    }

    /// <summary>
    /// Folds a sync result back into whatever is in local storage *now*.
    ///
    /// A sync run reads the local data, then spends a while on the network. If the
    /// user edits something in that window, writing the run's own snapshot back
    /// would silently discard that edit. So the run re-reads local storage right
    /// before writing and folds its result in with the same last-write-wins rule.
    /// A tie keeps the local record: it did not change while we were away, and
    /// keeping it avoids swapping object instances for no reason.
    /// </summary>
    /// <param name="current">What local storage holds right now.</param>
    /// <param name="incoming">The result the sync run computed.</param>
    /// <param name="drop">Ids of purged tombstones; they must not come back.</param>
    public static List<T> MergeRecords<T>(
        IReadOnlyList<T> current,
        IReadOnlyList<T> incoming,
        ISet<string>? drop = null)
        where T : class, ISyncable
    {
        var result = new List<T>(current.Count + incoming.Count);
        var index = new Dictionary<string, int>(StringComparer.Ordinal);

        void Add(T record)
        {
            index[record.Id] = result.Count;
            result.Add(record);
        }

        foreach (var record in current)
        {
            if (string.IsNullOrEmpty(record.Id) || drop?.Contains(record.Id) == true)
                continue;

            if (!index.ContainsKey(record.Id))
                Add(record);
        }

        foreach (var record in incoming)
        {
            if (string.IsNullOrEmpty(record.Id) || drop?.Contains(record.Id) == true)
                continue;

            if (index.TryGetValue(record.Id, out var position))
            {
                if (record.UpdatedAt > result[position].UpdatedAt)
                    result[position] = record;
            }
            else
            {
                Add(record);
            }
        }

        return result;
    }

    /// <summary>
    /// Removes tombstones older than <see cref="TombstoneLifetime"/> and reports
    /// their ids so the cloud copies can be removed as well.
    /// </summary>
    public static List<T> PurgeExpiredTombstones<T>(List<T> records, DateTime utcNow, out List<string> purgedIds)
        where T : ISyncable
    {
        purgedIds = new List<string>();
        var cutoff = utcNow - TombstoneLifetime;
        var kept = new List<T>(records.Count);

        foreach (var record in records)
        {
            if (record.IsDeleted && record.UpdatedAt != default && record.UpdatedAt < cutoff)
                purgedIds.Add(record.Id);
            else
                kept.Add(record);
        }

        return kept;
    }
}
