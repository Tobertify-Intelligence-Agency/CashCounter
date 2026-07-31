namespace CashCount.Shared.Models;

public class AccountLedger
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Accounts Overview";
    public string CurrencySymbol { get; set; } = "€";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<TrackedAccount> Accounts { get; set; } = new();
    public List<AccountTransaction> Transactions { get; set; } = new();

    public decimal TotalOpeningBalance => Accounts.Sum(a => a.OpeningBalance);
    public decimal TotalIncoming => Transactions.Where(t => t.Direction == CashFlowDirection.Incoming).Sum(t => t.Amount);
    public decimal TotalOutgoing => Transactions.Where(t => t.Direction == CashFlowDirection.Outgoing).Sum(t => t.Amount);
    public decimal NetFlow => TotalIncoming - TotalOutgoing;
    public decimal TotalBalance => TotalOpeningBalance + NetFlow;

    public decimal GetAccountBalance(string accountId)
    {
        var account = Accounts.FirstOrDefault(a => a.Id == accountId);
        if (account is null)
            return 0m;

        var incoming = Transactions
            .Where(t => t.AccountId == accountId && t.Direction == CashFlowDirection.Incoming)
            .Sum(t => t.Amount);

        var outgoing = Transactions
            .Where(t => t.AccountId == accountId && t.Direction == CashFlowDirection.Outgoing)
            .Sum(t => t.Amount);

        return account.OpeningBalance + incoming - outgoing;
    }

    public int GetTransactionCount(string accountId) => Transactions.Count(t => t.AccountId == accountId);

    public IEnumerable<CategorySummary> GetCategorySummaries()
    {
        return Transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.Category))
            .GroupBy(t => t.Category.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new CategorySummary
            {
                Category = group.First().Category.Trim(),
                Incoming = group.Where(t => t.Direction == CashFlowDirection.Incoming).Sum(t => t.Amount),
                Outgoing = group.Where(t => t.Direction == CashFlowDirection.Outgoing).Sum(t => t.Amount)
            })
            .OrderByDescending(summary => summary.TotalActivity)
            .ThenBy(summary => summary.Category);
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}

public class TrackedAccount : ISyncable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public AccountKind Kind { get; set; } = AccountKind.Cash;
    public decimal OpeningBalance { get; set; }
    public string Notes { get; set; } = string.Empty;

    /// <inheritdoc cref="ISyncable.UpdatedAt" />
    public DateTime UpdatedAt { get; set; }

    /// <inheritdoc cref="ISyncable.IsDeleted" />
    public bool IsDeleted { get; set; }
}

public class AccountTransaction : ISyncable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AccountId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public CashFlowDirection Direction { get; set; } = CashFlowDirection.Outgoing;
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
    public string Notes { get; set; } = string.Empty;
    public string? AttachmentDataUrl { get; set; }
    public string? AttachmentFileName { get; set; }
    public bool HasAttachment => !string.IsNullOrWhiteSpace(AttachmentDataUrl);

    /// <summary>
    /// Stable identity of the attached picture. Pictures are immutable: replacing
    /// a picture creates a new id. Cloud sync stores the picture itself under this
    /// id, so the transaction record stays small.
    /// </summary>
    public string? AttachmentId { get; set; }

    /// <inheritdoc cref="ISyncable.UpdatedAt" />
    public DateTime UpdatedAt { get; set; }

    /// <inheritdoc cref="ISyncable.IsDeleted" />
    public bool IsDeleted { get; set; }
}

public class CategorySummary
{
    public string Category { get; set; } = string.Empty;
    public decimal Incoming { get; set; }
    public decimal Outgoing { get; set; }
    public decimal Net => Incoming - Outgoing;
    public decimal TotalActivity => Incoming + Outgoing;
}

public enum CashFlowDirection
{
    Incoming,
    Outgoing
}

public enum AccountKind
{
    Cash,
    Bank,
    Savings,
    Investment,
    Fund,
    Other
}
