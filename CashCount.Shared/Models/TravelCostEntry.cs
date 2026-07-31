namespace CashCount.Shared.Models;

public class TravelCollection : ISyncable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = "€";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<TravelCostEntry> Entries { get; set; } = new();
    public string? LedgerTransactionId { get; set; }

    /// <inheritdoc cref="ISyncable.UpdatedAt" />
    public DateTime UpdatedAt { get; set; }

    /// <inheritdoc cref="ISyncable.IsDeleted" />
    public bool IsDeleted { get; set; }

    public decimal TotalIncome => Entries.Where(e => e.Type == EntryType.Income).Sum(e => e.Amount);
    public decimal TotalExpenses => Entries.Where(e => e.Type == EntryType.Expense).Sum(e => e.Amount);
    public decimal Balance => TotalIncome - TotalExpenses;
}

public class TravelCostEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public EntryType Type { get; set; } = EntryType.Expense;
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Today;
}

public enum EntryType
{
    Income,
    Expense
}
