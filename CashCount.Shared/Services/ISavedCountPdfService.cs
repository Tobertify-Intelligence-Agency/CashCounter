using CashCount.Shared.Models;

namespace CashCount.Shared.Services;

public interface ISavedCountPdfService
{
    byte[] GeneratePdf(SavedCount savedCount);
}
