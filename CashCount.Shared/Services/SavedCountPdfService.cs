using System.Globalization;
using System.Text;
using CashCount.Shared.Models;

namespace CashCount.Shared.Services;

public class SavedCountPdfService : ISavedCountPdfService
{
    public byte[] GeneratePdf(SavedCount savedCount)
    {
        ArgumentNullException.ThrowIfNull(savedCount);

        const float pageHeight = 842f;
        const float left = 48f;
        const float right = 547f;
        const float bottom = 52f;
        const float contentWidth = right - left;

        // Currency symbol may contain characters outside Latin-1 (e.g. € = U+20AC).
        // Fall back to the currency code so no '?' appears in the output.
        var rawSymbol = Sanitize(savedCount.CurrencySymbol);
        var safeSymbol = rawSymbol.Contains('?') ? savedCount.CurrencyCode : rawSymbol;
        string Money(decimal amount) => $"{safeSymbol} {amount.ToString("N2", CultureInfo.InvariantCulture)}";

        var pages = new List<StringBuilder>();
        var page = new StringBuilder();
        pages.Add(page);
        var y = pageHeight - 48f;

        void WriteTextAt(string text, float x, float ty, float fontSize, bool bold = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            page.AppendLine("BT");
            page.AppendLine($"/{(bold ? "F2" : "F1")} {Fmt(fontSize)} Tf");
            page.AppendLine($"1 0 0 1 {Fmt(x)} {Fmt(ty)} Tm");
            page.AppendLine($"({EscapePdfText(Sanitize(text))}) Tj");
            page.AppendLine("ET");
        }

        void WriteText(string text, float x, float fontSize, bool bold = false)
            => WriteTextAt(text, x, y, fontSize, bold);

        void HRule(float lineY, float w = 0.5f)
        {
            page.AppendLine($"{Fmt(w)} w");
            page.AppendLine($"{Fmt(left)} {Fmt(lineY)} m");
            page.AppendLine($"{Fmt(right)} {Fmt(lineY)} l S");
        }

        void VLine(float x, float y1, float y2, float w = 0.5f)
        {
            page.AppendLine($"{Fmt(w)} w");
            page.AppendLine($"{Fmt(x)} {Fmt(y1)} m");
            page.AppendLine($"{Fmt(x)} {Fmt(y2)} l S");
        }

        void EnsureSpace(float h)
        {
            if (y - h >= bottom) return;
            page = new StringBuilder();
            pages.Add(page);
            y = pageHeight - 48f;
        }

        // ── 1. HEADER ────────────────────────────────────────────────────────
        WriteText(savedCount.Name, left, 20f, true);
        WriteTextAt(savedCount.SavedAt.ToString("dd.MM.yyyy  HH:mm", CultureInfo.InvariantCulture), right - 110f, y, 10f);
        y -= 10f;
        HRule(y, 1f);
        y -= 18f;

        // ── 2. TOTAL ─────────────────────────────────────────────────────────
        EnsureSpace(28f);
        WriteText("Total", left, 9f, true);
        WriteText(Money(savedCount.TotalAmount), left + 60f, 16f, true);
        y -= 22f;

        // ── 3. META ──────────────────────────────────────────────────────────
        EnsureSpace(18f);
        WriteText($"Currency: {savedCount.CurrencyCode}", left, 9f);
        WriteTextAt($"Saved: {savedCount.SavedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)}", left + 160f, y, 9f);
        y -= 16f;
        HRule(y);
        y -= 14f;

        // row padding constants — rowTop must exceed the font cap-height (~8pt for 10pt)
        // so rules never cut through letter tops; rowBot clears descenders (~2pt)
        const float rowTop = 11f;
        const float rowBot = 5f;

        // ── 4. TABLE HEADER ───────────────────────────────────────────────────
        EnsureSpace(rowTop + rowBot + 2f);
        y -= rowTop;
        WriteText("Denomination", left, 9f, true);
        WriteTextAt("Qty", left + 290f, y, 9f, true);
        WriteTextAt("Amount", right - 80f, y, 9f, true);
        y -= rowBot;
        HRule(y, 0.75f);

        // ── 5. DENOMINATION ROWS ──────────────────────────────────────────────
        var allDenoms = savedCount.Denominations
            .OrderBy(d => d.IsCoin)
            .ThenByDescending(d => d.Value)
            .ToList();

        if (allDenoms.Count == 0)
        {
            EnsureSpace(rowTop + rowBot);
            y -= rowTop;
            WriteText("No denominations recorded.", left, 10f);
            y -= rowBot;
            HRule(y, 0.3f);
        }
        else
        {
            foreach (var denom in allDenoms)
            {
                EnsureSpace(rowTop + rowBot);
                y -= rowTop;
                WriteText(denom.DisplayName, left, 10f);
                WriteTextAt($"x {denom.Quantity}", left + 290f, y, 10f);
                WriteTextAt(Money(denom.Value * denom.Quantity), right - 80f, y, 10f);
                y -= rowBot;
                HRule(y, 0.3f);
            }
        }

        // ── 6. TOTAL ROW ──────────────────────────────────────────────────────
        EnsureSpace(rowTop + rowBot + 4f);
        y -= rowTop;
        WriteText("Total", left, 10f, true);
        WriteTextAt(Money(savedCount.TotalAmount), right - 80f, y, 11f, true);
        y -= rowBot;
        HRule(y, 1f);
        y -= 18f;

        // ── 7. SIGNATURES ─────────────────────────────────────────────────────
        EnsureSpace(16f);
        WriteText("Signatures", left, 10f, true);
        y -= 6f;
        HRule(y, 0.75f);
        y -= 8f;

        void RenderSignatureBox(SavedCountSignature sig, string fallbackLabel)
        {
            const float boxH = 80f;
            EnsureSpace(boxH + 4f);
            var boxBottom = y - boxH;

            page.AppendLine("0.5 w");
            page.AppendLine($"{Fmt(left)} {Fmt(boxBottom)} {Fmt(contentWidth)} {Fmt(boxH)} re S");

            if (sig.HasAnySignature)
            {
                var signer = string.IsNullOrWhiteSpace(sig.SignerName) ? "Signer" : sig.SignerName.Trim();
                var signedOn = sig.SignedAt.HasValue
                    ? $"Signed on {sig.SignedAt.Value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)}"
                    : "Signed";
                WriteTextAt(signer, left + 10f, boxBottom + boxH - 18f, 11f, true);
                WriteTextAt(signedOn, left + 10f, boxBottom + boxH - 31f, 9f);
            }
            else
            {
                WriteTextAt(fallbackLabel, left + 10f, boxBottom + boxH - 18f, 9f);
            }

            var divX = left + contentWidth * 0.42f;
            VLine(divX, boxBottom + 8f, boxBottom + boxH - 8f, 0.4f);

            var sigLeft = divX + 10f;
            var sigWidth = right - sigLeft - 10f;
            var sigBaseline = boxBottom + 14f;

            page.AppendLine("0.4 w");
            page.AppendLine($"{Fmt(sigLeft)} {Fmt(sigBaseline)} m");
            page.AppendLine($"{Fmt(right - 10f)} {Fmt(sigBaseline)} l S");

            if (sig.HasDrawnSignature)
                RenderDrawnSignature(page, sig.DrawnStrokes, sigLeft, sigBaseline + 2f, sigWidth, boxH - 24f);
            else if (sig.HasTypedSignature)
                WriteTextAt(sig.TypedSignature, sigLeft + 4f, sigBaseline + 14f, 16f);

            y = boxBottom - 10f;
        }

        RenderSignatureBox(savedCount.Signature ?? new SavedCountSignature(), "Signer 1 — not signed");
        RenderSignatureBox(savedCount.SecondSignature ?? new SavedCountSignature(), "Signer 2 — not signed");

        // ── 8. FOOTER on every page ───────────────────────────────────────────
        var footerTs = savedCount.SavedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        foreach (var pg in pages)
        {
            pg.AppendLine("0.6 0.6 0.6 RG");
            pg.AppendLine($"0.4 w {Fmt(left)} 34 m {Fmt(right)} 34 l S");
            pg.AppendLine("0 0 0 RG");
            pg.AppendLine("0.5 0.5 0.5 rg");
            pg.AppendLine($"BT /F1 7.5 Tf 1 0 0 1 {Fmt(left)} 22 Tm (Generated by CashCount) Tj ET");
            pg.AppendLine($"BT /F1 7.5 Tf 1 0 0 1 {Fmt(right - 80f)} 22 Tm ({EscapePdfText(Sanitize(footerTs))}) Tj ET");
            pg.AppendLine("0 0 0 rg");
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

    private static string Sanitize(string value)
    {
        var sb = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            if (ch == '\u20AC') sb.Append("EUR");       // € not in Latin-1
            else if (ch <= 255) sb.Append(ch);
            else sb.Append('?');
        }
        return sb.ToString();
    }

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
