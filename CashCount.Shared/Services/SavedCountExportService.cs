using CashCount.Shared.Models;

namespace CashCount.Shared.Services;

public class SavedCountExportService
{
    private readonly ISavedCountPdfService _pdfService;
    private readonly IFileExportService _fileExportService;

    public SavedCountExportService(ISavedCountPdfService pdfService, IFileExportService fileExportService)
    {
        _pdfService = pdfService;
        _fileExportService = fileExportService;
    }

    public async Task ExportAsync(SavedCount savedCount)
    {
        var fileName = BuildFileName(savedCount);
        var content = _pdfService.GeneratePdf(savedCount);
        await _fileExportService.ExportPdfAsync(fileName, content);
    }

    public static string BuildFileName(SavedCount savedCount)
    {
        var name = string.IsNullOrWhiteSpace(savedCount.Name) ? "cash-count" : savedCount.Name.Trim().ToLowerInvariant();
        var safe = string.Concat(name.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-'));
        while (safe.Contains("--", StringComparison.Ordinal))
        {
            safe = safe.Replace("--", "-", StringComparison.Ordinal);
        }
        safe = safe.Trim('-');
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "cash-count";
        }

        return $"{safe}-{savedCount.SavedAt:yyyyMMdd-HHmm}.pdf";
    }
}
