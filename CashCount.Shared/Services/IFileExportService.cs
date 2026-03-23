namespace CashCount.Shared.Services;

public interface IFileExportService
{
    Task ExportPdfAsync(string fileName, byte[] content);
}
