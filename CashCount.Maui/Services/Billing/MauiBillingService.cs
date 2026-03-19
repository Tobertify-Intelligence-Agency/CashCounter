using CashCount.Shared.Services.Billing;
using Plugin.InAppBilling;

namespace CashCount.Maui.Services.Billing;

/// <summary>
/// MAUI implementation of IBillingService using Plugin.InAppBilling.
/// Supports Android (Google Play) and iOS/macOS (App Store).
/// Windows does not support in-app purchases via this plugin.
/// </summary>
public class MauiBillingService : IBillingService
{
    public event EventHandler<PurchaseResult>? PurchaseCompleted;

    public Task<bool> IsAvailableAsync()
    {
#if ANDROID || IOS || MACCATALYST
        return Task.FromResult(CrossInAppBilling.IsSupported);
#else
        return Task.FromResult(false);
#endif
    }

    public async Task<List<ProductInfo>> GetProductsAsync()
    {
#if ANDROID || IOS || MACCATALYST
        var billing = CrossInAppBilling.Current;
        try
        {
            var connected = await billing.ConnectAsync();
            if (!connected)
                return new List<ProductInfo>();

            var products = await billing.GetProductInfoAsync(
                ItemType.InAppPurchase,
                new[] { ProductIds.Premium });

            return products?.Select(p => new ProductInfo
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                FormattedPrice = p.LocalizedPrice,
                CurrencyCode = p.CurrencyCode
            }).ToList() ?? new List<ProductInfo>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetProductsAsync error: {ex.Message}");
            return new List<ProductInfo>();
        }
        finally
        {
            await billing.DisconnectAsync();
        }
#else
        return await Task.FromResult(new List<ProductInfo>());
#endif
    }

    public async Task<PurchaseResult> PurchaseAsync(string productId)
    {
#if ANDROID || IOS || MACCATALYST
        var billing = CrossInAppBilling.Current;
        try
        {
            var connected = await billing.ConnectAsync();
            if (!connected)
                return PurchaseResult.Failed("Could not connect to the store. Please check your internet connection.");

            var purchase = await billing.PurchaseAsync(productId, ItemType.InAppPurchase);

            if (purchase == null)
                return PurchaseResult.Cancelled();

            // Finalize pending transactions (v5+ API, safe to call even if not pending)
            if (purchase.State == Plugin.InAppBilling.PurchaseState.PaymentPending)
            {
                await billing.FinalizePurchaseAsync(new[] { purchase.PurchaseToken });
            }

            var result = PurchaseResult.Succeeded(purchase.ProductId, purchase.Id);
            PurchaseCompleted?.Invoke(this, result);
            return result;
        }
        catch (InAppBillingPurchaseException ex)
        {
            return ex.PurchaseError switch
            {
                PurchaseError.UserCancelled => PurchaseResult.Cancelled(),
                PurchaseError.AlreadyOwned => HandleAlreadyOwned(productId),
                _ => PurchaseResult.Failed(GetErrorMessage(ex.PurchaseError))
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PurchaseAsync error: {ex.Message}");
            return PurchaseResult.Failed("An unexpected error occurred. Please try again.");
        }
        finally
        {
            await billing.DisconnectAsync();
        }
#else
        return await Task.FromResult(PurchaseResult.Failed(
            "In-app purchases are only available on iOS and Android."));
#endif
    }

    public async Task<bool> RestorePurchasesAsync()
    {
#if ANDROID || IOS || MACCATALYST
        var billing = CrossInAppBilling.Current;
        try
        {
            var connected = await billing.ConnectAsync();
            if (!connected)
                return false;

            var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);

            var premiumPurchase = purchases?.FirstOrDefault(p =>
                p.ProductId == ProductIds.Premium &&
                (p.State == Plugin.InAppBilling.PurchaseState.Purchased ||
                 p.State == Plugin.InAppBilling.PurchaseState.Restored));

            if (premiumPurchase != null)
            {
                var result = PurchaseResult.Restored(premiumPurchase.ProductId);
                PurchaseCompleted?.Invoke(this, result);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RestorePurchasesAsync error: {ex.Message}");
            return false;
        }
        finally
        {
            await billing.DisconnectAsync();
        }
#else
        return await Task.FromResult(false);
#endif
    }

    private PurchaseResult HandleAlreadyOwned(string productId)
    {
        // Product already purchased — treat as successful restore
        var result = PurchaseResult.Restored(productId);
        PurchaseCompleted?.Invoke(this, result);
        return result;
    }

    private static string GetErrorMessage(PurchaseError error) => error switch
    {
        PurchaseError.BillingUnavailable => "Billing is currently unavailable. Please try again later.",
        PurchaseError.DeveloperError => "A configuration error occurred.",
        PurchaseError.ItemUnavailable => "This product is not available in your region.",
        PurchaseError.GeneralError => "A general error occurred. Please try again.",
        PurchaseError.ServiceUnavailable => "Store service is unavailable. Check your internet connection.",
        PurchaseError.AppStoreUnavailable => "The App Store is currently unavailable.",
        PurchaseError.PaymentNotAllowed => "Payments are not allowed on this device.",
        PurchaseError.PaymentInvalid => "The payment information is invalid.",
        PurchaseError.InvalidProduct => "This product is invalid.",
        _ => "An unexpected error occurred. Please try again."
    };
}
