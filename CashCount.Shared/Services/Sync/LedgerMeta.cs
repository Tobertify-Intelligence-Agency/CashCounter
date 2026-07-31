using CashCount.Shared.Models;

namespace CashCount.Shared.Services.Sync;

/// <summary>
/// The ledger's own fields (everything except the two child lists) as a syncable
/// record.
///
/// The ledger is not stored as one big document, because two devices that each
/// add one transaction would then overwrite each other wholesale. Accounts and
/// transactions are synced as individual records; what is left over — the name,
/// the currency symbol — travels in this single document with the fixed id
/// <see cref="SyncCollections.LedgerMetaId"/>.
/// </summary>
public sealed class LedgerMeta : ISyncable
{
    public string Id { get; set; } = SyncCollections.LedgerMetaId;
    public string LedgerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = "€";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Always false: the ledger itself is never deleted, only emptied.</summary>
    public bool IsDeleted { get; set; }

    public static LedgerMeta From(AccountLedger ledger) => new()
    {
        Id = SyncCollections.LedgerMetaId,
        LedgerId = ledger.Id,
        Name = ledger.Name,
        CurrencySymbol = ledger.CurrencySymbol,
        CreatedAt = ledger.CreatedAt,
        UpdatedAt = ledger.UpdatedAt
    };

    public void ApplyTo(AccountLedger ledger)
    {
        ledger.Name = Name;
        ledger.CurrencySymbol = CurrencySymbol;
        ledger.UpdatedAt = UpdatedAt;

        if (CreatedAt != default)
            ledger.CreatedAt = CreatedAt;
    }
}
