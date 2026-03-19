namespace CashCount.Shared.Models;

public class SavedCount
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal BanknotesTotal { get; set; }
    public decimal CoinsTotal { get; set; }
    public List<DenominationCount> Denominations { get; set; } = new();
}

public class DenominationCount
{
    public decimal Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsCoin { get; set; }
}
