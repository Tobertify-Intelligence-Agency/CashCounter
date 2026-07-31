using CashCount.Shared.Models;

namespace CashCount.Shared.Services.Sync;

/// <summary>
/// Brings records saved before sync existed into a shape the merge algorithm can
/// reason about, and keeps every timestamp in UTC.
///
/// Back-filling matters more than it looks: if an undated record were simply
/// stamped with "now" at load time, then every device would consider its own copy
/// the newest one every single time it started, and the last device to open the
/// app would always win. Deriving the timestamp from the record's own creation
/// date gives the same answer on every device.
/// </summary>
public static class SyncNormalization
{
    public static DateTime AsUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static bool NormalizeCounts(List<SavedCount> counts)
    {
        var changed = false;

        foreach (var count in counts)
        {
            var original = count.UpdatedAt;
            count.UpdatedAt = count.UpdatedAt == default
                ? AsUtc(count.SavedAt)
                : AsUtc(count.UpdatedAt);

            if (count.UpdatedAt != original)
                changed = true;
        }

        return changed;
    }

    public static bool NormalizeTrips(List<TravelCollection> trips)
    {
        var changed = false;

        foreach (var trip in trips)
        {
            var original = trip.UpdatedAt;
            trip.UpdatedAt = trip.UpdatedAt == default
                ? AsUtc(trip.CreatedAt)
                : AsUtc(trip.UpdatedAt);

            if (trip.UpdatedAt != original)
                changed = true;
        }

        return changed;
    }

    /// <param name="assignAttachmentIds">
    /// Whether a picture without an id gets a fresh one. A sync run re-reads the
    /// ledger just before writing, and that second pass must not mint ids again:
    /// it would hand the very same picture a second id, and the copy already in the
    /// cloud under the first id would be stranded there. The run carries the ids
    /// from its own first pass over instead.
    /// </param>
    public static bool NormalizeLedger(AccountLedger ledger, bool assignAttachmentIds = true)
    {
        var changed = false;

        var ledgerStamp = ledger.UpdatedAt == default
            ? AsUtc(ledger.CreatedAt)
            : AsUtc(ledger.UpdatedAt);

        if (ledgerStamp != ledger.UpdatedAt)
        {
            ledger.UpdatedAt = ledgerStamp;
            changed = true;
        }

        foreach (var account in ledger.Accounts)
        {
            var original = account.UpdatedAt;
            account.UpdatedAt = account.UpdatedAt == default ? ledgerStamp : AsUtc(account.UpdatedAt);

            if (account.UpdatedAt != original)
                changed = true;
        }

        foreach (var transaction in ledger.Transactions)
        {
            var original = transaction.UpdatedAt;
            transaction.UpdatedAt = transaction.UpdatedAt == default ? ledgerStamp : AsUtc(transaction.UpdatedAt);

            if (transaction.UpdatedAt != original)
                changed = true;

            // A picture that predates sync has no id yet; give it one so it can be
            // stored in the cloud under a stable name.
            if (assignAttachmentIds && transaction.HasAttachment && string.IsNullOrEmpty(transaction.AttachmentId))
            {
                transaction.AttachmentId = Guid.NewGuid().ToString("N");
                changed = true;
            }
        }

        return changed;
    }
}
