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
    public SavedCountSignature Signature { get; set; } = new();
}

public class DenominationCount
{
    public decimal Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsCoin { get; set; }
}

public class SavedCountSignature
{
    public string SignerName { get; set; } = string.Empty;
    public string TypedSignature { get; set; } = string.Empty;
    public SignatureMode Mode { get; set; } = SignatureMode.Drawn;
    public DateTime? SignedAt { get; set; }
    public List<SignatureStroke> DrawnStrokes { get; set; } = new();

    public bool HasTypedSignature => !string.IsNullOrWhiteSpace(TypedSignature);
    public bool HasDrawnSignature => DrawnStrokes.Any(s => s.Points.Count > 0);
    public bool HasAnySignature => HasDrawnSignature || HasTypedSignature || !string.IsNullOrWhiteSpace(SignerName);
}

public enum SignatureMode
{
    Drawn = 0,
    Typed = 1
}

public class SignatureStroke
{
    public List<SignaturePoint> Points { get; set; } = new();
}

public class SignaturePoint
{
    public double X { get; set; }
    public double Y { get; set; }
}
