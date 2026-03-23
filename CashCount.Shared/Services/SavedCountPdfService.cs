using System.Globalization;
using System.Text;
using CashCount.Shared.Models;

namespace CashCount.Shared.Services;

public class SavedCountPdfService : ISavedCountPdfService
{
    public byte[] GeneratePdf(SavedCount savedCount)
    {
        ArgumentNullException.ThrowIfNull(savedCount);

        const float left = 48f;
        const float right = 547f;
        const float top = 792f;
        const float bottom = 52f;
        const float contentWidth = right - left;

        var pages = new List<StringBuilder>();
        var page = new StringBuilder();
        pages.Add(page);

        var y = top;

        void EnsureSpace(float heightNeeded)
        {
            if (y - heightNeeded < bottom)
            {
                page = new StringBuilder();
                pages.Add(page);
                y = top;
            }
        }

        void WriteText(string text, float x, float fontSize, bool bold = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            page.AppendLine("BT");
            page.AppendLine($"/{(bold ? "F2" : "F1")} {Fmt(fontSize)} Tf");
            page.AppendLine($"1 0 0 1 {Fmt(x)} {Fmt(y)} Tm");
            page.AppendLine($"({EscapePdfText(Sanitize(text))}) Tj");
            page.AppendLine("ET");
        }

        void DrawLine(float x1, float y1, float x2, float y2, float width = 1f)
        {
            page.AppendLine($"{Fmt(width)} w");
            page.AppendLine($"{Fmt(x1)} {Fmt(y1)} m");
            page.AppendLine($"{Fmt(x2)} {Fmt(y2)} l S");
        }

        void AddLine(string label, string value, float fontSize = 11f)
        {
            EnsureSpace(18f);
            WriteText(label, left, fontSize, true);
            WriteText(value, left + 120f, fontSize);
            y -= 18f;
        }

        EnsureSpace(30f);
        WriteText(savedCount.Name, left, 22f, true);
        y -= 28f;
        WriteText("Saved cash count export", left, 11f);
        y -= 22f;
        DrawLine(left, y, right, y, 1f);
        y -= 20f;

        AddLine("Saved on", savedCount.SavedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture));
        AddLine("Currency", $"{savedCount.CurrencySymbol} {savedCount.CurrencyCode}".Trim());
        AddLine("Banknotes", FormatMoney(savedCount.CurrencySymbol, savedCount.BanknotesTotal));
        AddLine("Coins", FormatMoney(savedCount.CurrencySymbol, savedCount.CoinsTotal));
        AddLine("Total", FormatMoney(savedCount.CurrencySymbol, savedCount.TotalAmount), 12f);

        y -= 10f;
        EnsureSpace(22f);
        WriteText("Denomination breakdown", left, 14f, true);
        y -= 20f;

        var orderedDenominations = savedCount.Denominations
            .OrderBy(d => d.IsCoin)
            .ThenByDescending(d => d.Value)
            .ToList();

        if (orderedDenominations.Count == 0)
        {
            EnsureSpace(16f);
            WriteText("No denominations were stored for this count.", left, 11f);
            y -= 16f;
        }
        else
        {
            foreach (var denomination in orderedDenominations)
            {
                EnsureSpace(18f);
                WriteText($"{denomination.Quantity} × {denomination.DisplayName}", left, 11f);
                WriteText(FormatMoney(savedCount.CurrencySymbol, denomination.Value * denomination.Quantity), left + 260f, 11f);
                y -= 18f;
            }
        }

        if (savedCount.Signature?.HasAnySignature == true)
        {
            var signature = savedCount.Signature;
            y -= 12f;
            EnsureSpace(170f);
            WriteText("Signature", left, 14f, true);
            y -= 16f;

            var boxTop = y;
            var boxHeight = 108f;
            var boxBottom = boxTop - boxHeight;
            page.AppendLine("0.75 w");
            page.AppendLine($"{Fmt(left)} {Fmt(boxBottom)} {Fmt(contentWidth)} {Fmt(boxHeight)} re S");

            var signer = string.IsNullOrWhiteSpace(signature.SignerName) ? "Signer" : signature.SignerName.Trim();
            var signedLabel = signature.SignedAt.HasValue
                ? $"Signed on {signature.SignedAt.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)}"
                : "Signed";

            y = boxTop - 20f;
            WriteText($"Signer: {signer}", left + 12f, 11f, true);
            y -= 16f;
            WriteText(signedLabel, left + 12f, 10f);

            var signatureAreaLeft = left + 12f;
            var signatureAreaBottom = boxBottom + 18f;
            var signatureAreaWidth = contentWidth - 24f;
            var signatureAreaHeight = 42f;

            DrawLine(signatureAreaLeft, signatureAreaBottom, signatureAreaLeft + signatureAreaWidth, signatureAreaBottom, 0.8f);

            if (signature.HasDrawnSignature)
            {
                RenderDrawnSignature(page, signature.DrawnStrokes, signatureAreaLeft, signatureAreaBottom + 4f, signatureAreaWidth, signatureAreaHeight - 8f);
            }
            else if (signature.HasTypedSignature)
            {
                y = signatureAreaBottom + 12f;
                WriteText(signature.TypedSignature, signatureAreaLeft + 4f, 18f);
            }

            y = boxBottom - 24f;
        }
        else
        {
            y -= 18f;
            EnsureSpace(36f);
            WriteText("Signature", left, 14f, true);
            y -= 14f;
            DrawLine(left, y, left + 220f, y, 0.8f);
            y -= 18f;
        }

        return SimplePdfDocument.Create(pages);
    }

    private static void RenderDrawnSignature(StringBuilder page, List<SignatureStroke> strokes, float left, float bottom, float width, float height)
    {
        const double sourceWidth = 560d;
        const double sourceHeight = 180d;

        var scaleX = width / sourceWidth;
        var scaleY = height / sourceHeight;

        page.AppendLine("0 0 0 RG");
        page.AppendLine("1.2 w");

        foreach (var stroke in strokes.Where(s => s.Points.Count > 0))
        {
            var first = stroke.Points[0];
            page.AppendLine($"{Fmt(left + (float)(first.X * scaleX))} {Fmt(bottom + height - (float)(first.Y * scaleY))} m");
            foreach (var point in stroke.Points.Skip(1))
            {
                page.AppendLine($"{Fmt(left + (float)(point.X * scaleX))} {Fmt(bottom + height - (float)(point.Y * scaleY))} l");
            }
            page.AppendLine("S");
        }
    }

    private static string FormatMoney(string symbol, decimal amount)
        => string.IsNullOrWhiteSpace(symbol)
            ? amount.ToString("N2", CultureInfo.InvariantCulture)
            : $"{symbol} {amount.ToString("N2", CultureInfo.InvariantCulture)}";

    private static string Sanitize(string value)
        => new(value.Select(ch => ch <= 255 ? ch : '?').ToArray());

    private static string EscapePdfText(string text)
        => text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);

    private static string Fmt(float value)
        => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static class SimplePdfDocument
    {
        public static byte[] Create(IReadOnlyList<StringBuilder> pages)
        {
            var body = new MemoryStream();
            var offsets = new List<long> { 0 };
            using var writer = new StreamWriter(body, Encoding.Latin1, 1024, leaveOpen: true);

            void WriteObject(int number, string content)
            {
                writer.Flush();
                offsets.Add(body.Position);
                writer.Write($"{number} 0 obj\n{content}\nendobj\n");
                writer.Flush();
            }

            var boldFontObject = 3 + (pages.Count * 2) + 1;
            var pageObjectNumbers = new List<int>();
            var contentObjectNumbers = new List<int>();
            var nextObjectNumber = 4;

            for (var i = 0; i < pages.Count; i++)
            {
                contentObjectNumbers.Add(nextObjectNumber++);
                pageObjectNumbers.Add(nextObjectNumber++);
            }

            WriteObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
            WriteObject(2, $"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(' ', pageObjectNumbers.Select(number => $"{number} 0 R"))}] >>");
            WriteObject(3, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
            WriteObject(boldFontObject, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

            for (var i = 0; i < pages.Count; i++)
            {
                var streamContent = pages[i].ToString();
                WriteObject(contentObjectNumbers[i], $"<< /Length {Encoding.ASCII.GetByteCount(streamContent)} >>\nstream\n{streamContent}endstream");
                WriteObject(pageObjectNumbers[i], $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 {boldFontObject} 0 R >> >> /Contents {contentObjectNumbers[i]} 0 R >>");
            }

            writer.Flush();
            var xrefPosition = body.Position;
            writer.Write($"xref\n0 {offsets.Count}\n");
            writer.Write("0000000000 65535 f \n");
            for (var i = 1; i < offsets.Count; i++)
            {
                writer.Write($"{offsets[i]:D10} 00000 n \n");
            }
            writer.Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF");
            writer.Flush();

            using var output = new MemoryStream();
            output.Write(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });
            body.Position = 0;
            body.CopyTo(output);
            return output.ToArray();
        }
    }
}
