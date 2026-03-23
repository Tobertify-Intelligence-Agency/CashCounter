using Microsoft.JSInterop;
using CashCount.Shared.Services;

namespace CashCount.Web.Services;

public class WebFileExportService : IFileExportService
{
    private readonly IJSRuntime _jsRuntime;

    public WebFileExportService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task ExportPdfAsync(string fileName, byte[] content)
    {
        await _jsRuntime.InvokeVoidAsync("cashCountExports.downloadFile", fileName, "application/pdf", content);
    }
}
