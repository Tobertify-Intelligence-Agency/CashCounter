namespace CashCount.Shared.Services.Billing;

/// <summary>
/// Service for in-app purchases.
/// </summary>
public interface IBillingService
{
    /// <summary>
    /// Check if billing is available on this platform.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get available products for purchase.
    /// </summary>
    Task<List<ProductInfo>> GetProductsAsync();

    /// <summary>
    /// Purchase a product by ID.
    /// </summary>
    Task<PurchaseResult> PurchaseAsync(string productId);

    /// <summary>
    /// Restore previous purchases (for re-installing app or new device).
    /// </summary>
    Task<bool> RestorePurchasesAsync();

    /// <summary>
    /// Event fired when a purchase is completed.
    /// </summary>
    event EventHandler<PurchaseResult>? PurchaseCompleted;
}

/// <summary>
/// Information about a purchasable product.
/// </summary>
public class ProductInfo
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string FormattedPrice { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
}

/// <summary>
/// Result of a purchase operation.
/// </summary>
public class PurchaseResult
{
    public bool Success { get; set; }
    public string? ProductId { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
    public PurchaseState State { get; set; }

    public static PurchaseResult Succeeded(string productId, string transactionId) => new()
    {
        Success = true,
        ProductId = productId,
        TransactionId = transactionId,
        State = PurchaseState.Purchased
    };

    public static PurchaseResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message,
        State = PurchaseState.Failed
    };

    public static PurchaseResult Cancelled() => new()
    {
        Success = false,
        State = PurchaseState.Cancelled
    };

    public static PurchaseResult Restored(string productId) => new()
    {
        Success = true,
        ProductId = productId,
        State = PurchaseState.Restored
    };
}

/// <summary>
/// State of a purchase.
/// </summary>
public enum PurchaseState
{
    Unknown,
    Purchased,
    Pending,
    Failed,
    Cancelled,
    Restored
}

/// <summary>
/// Product IDs for in-app purchases.
/// </summary>
public static class ProductIds
{
    public const string Premium = "com.cashcount.premium";
}
