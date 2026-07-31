namespace CashCount.Shared.Services.Sync;

/// <summary>
/// One record as it is stored in the cloud.
///
/// The record's actual content travels as a JSON string in <see cref="Payload"/>
/// rather than as native cloud fields. That is deliberate:
/// <list type="bullet">
///   <item><description>Firestore cannot store an array inside an array, and
///   <c>SavedCount.Signature.DrawnStrokes[].Points[]</c> is exactly that — a
///   field-by-field mapping would silently break signatures.</description></item>
///   <item><description>The sync layer stays model-agnostic: adding a field to a
///   model needs no change in the Android or Web storage implementations.</description></item>
/// </list>
/// Only the three fields the merge algorithm needs are stored natively.
/// </summary>
public sealed class SyncDocument
{
    /// <summary>Record id; used as the cloud document id.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>UTC time of the last change to this record.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>True for a tombstone; <see cref="Payload"/> is then empty.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>The record serialised as JSON, or empty for a tombstone.</summary>
    public string Payload { get; set; } = string.Empty;
}

/// <summary>
/// Cloud sub-collection names under <c>users/{uid}/</c>.
/// </summary>
public static class SyncCollections
{
    public const string Counts = "counts";
    public const string Trips = "trips";
    public const string LedgerMeta = "ledger";
    public const string Accounts = "accounts";
    public const string Transactions = "transactions";
    public const string Attachments = "attachments";

    /// <summary>Document id of the single ledger metadata document.</summary>
    public const string LedgerMetaId = "main";
}
