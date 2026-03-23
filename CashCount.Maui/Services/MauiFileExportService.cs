using CashCount.Shared.Services;

namespace CashCount.Maui.Services;

public class MauiFileExportService : IFileExportService
{
    public async Task ExportPdfAsync(string fileName, byte[] content)
    {
        var path = Path.Combine(FileSystem.Current.CacheDirectory, fileName);
        await File.WriteAllBytesAsync(path, content);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Export cash count PDF",
            File = new ShareFile(path, "application/pdf")
        });
    }
}
