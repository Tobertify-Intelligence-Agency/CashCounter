namespace CashCount.Shared.Models;

/// <summary>
/// A record that can be synchronised between devices.
///
/// Two fields carry the whole synchronisation contract:
/// <list type="bullet">
///   <item><description><see cref="UpdatedAt"/> — UTC time of the last change.
///   Conflicts between devices are resolved by taking the newer value
///   (last write wins, per record — never per list).</description></item>
///   <item><description><see cref="IsDeleted"/> — a tombstone. Deleted records
///   are kept (hidden from the UI) so the deletion can travel to the other
///   devices. Without it, a record deleted on device A simply reappears from
///   the cloud on the next sync, because "missing locally" and "deleted"
///   would be indistinguishable.</description></item>
/// </list>
///
/// <see cref="UpdatedAt"/> defaults to <c>default(DateTime)</c> on purpose:
/// records written before sync existed carry no timestamp, and the sync layer
/// back-fills them from their creation date instead of stamping "now" (which
/// would make every old record look brand new on every device).
/// </summary>
public interface ISyncable
{
    /// <summary>Stable id of the record, identical on every device.</summary>
    string Id { get; set; }

    /// <summary>UTC time of the last change; <c>default</c> when unknown.</summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>True when the record was deleted and is only kept as a tombstone.</summary>
    bool IsDeleted { get; set; }
}
