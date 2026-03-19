using CashCount.Shared.Services.Billing;

namespace CashCount.Web.Services.Billing;

/// <summary>
/// Web implementation of IBillingService.
/// In-app purchases are not available on the web - directs users to mobile apps.
/// </summary>
public class WebBillingService : IBillingService
{
    public event EventHandler<PurchaseResult>? PurchaseCompleted;

    public Task<bool> IsAvailableAsync()
    {
        // Billing is not available on web
        return Task.FromResult(false);
    }

    public Task<List<ProductInfo>> GetProductsAsync()
    {
        // Return empty list - no products available on web
        return Task.FromResult(new List<ProductInfo>());
    }

    public Task<PurchaseResult> PurchaseAsync(string productId)
    {
        // Inform user to use mobile app
        return Task.FromResult(new PurchaseResult
        {
            Success = false,
            ProductId = productId,
            State = PurchaseState.Failed,
            ErrorMessage = "In-app purchases are only available on iOS and Android. " +
                          "Please download the CashCount app from the App Store or Google Play to upgrade to Premium."
        });
    }

    public Task<bool> RestorePurchasesAsync()
    {
        // Cannot restore purchases on web
        return Task.FromResult(false);
    }
}
