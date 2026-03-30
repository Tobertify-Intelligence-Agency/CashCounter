using System.Globalization;
using System.Text;
using CashCount.Shared.Models;
using CashCount.Shared.Services.Localization;

namespace CashCount.Shared.Services;

public class SavedCountReceiptService
{
    private readonly IFileExportService _fileExportService;
    private readonly IAppTextService _text;

    public SavedCountReceiptService(IFileExportService fileExportService, IAppTextService text)
    {
        _fileExportService = fileExportService;
        _text = text;
    }

    public async Task ExportAsync(SavedCount savedCount)
    {
        ArgumentNullException.ThrowIfNull(savedCount);
        var fileName = BuildFileName(savedCount, _text.CurrentCulture);
        var content = GenerateReceiptPdf(savedCount);
        await _fileExportService.ExportPdfAsync(fileName, content);
    }

    public static string BuildFileName(SavedCount savedCount, CultureInfo? culture = null)
    {
        var ts = savedCount.SavedAt.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        var prefix = (culture ?? CultureInfo.InvariantCulture).TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase) ? "quittung" : "receipt";
        return $"{prefix}-{ts}.pdf";
    }

    public static byte[] GenerateReceiptPdf(SavedCount savedCount)
    {
        const float pageHeight = 842f;
        const float left = 60f;
        const float right = 535f;
        const float bottom = 52f;
        const float contentWidth = right - left;
        const float centerX = (left + right) / 2f;

        var rawSymbol = Sanitize(savedCount.CurrencySymbol);
        var safeSymbol = rawSymbol.Contains('?') ? savedCount.CurrencyCode : rawSymbol;
        string Money(decimal amount) => $"{safeSymbol} {amount.ToString("N2", _text.CurrentCulture)}";
        string T(string key) => _text[key];
        var isGerman = _text.CurrentCulture.TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase);

        var pages = new List<StringBuilder>();
        var page = new StringBuilder();
        pages.Add(page);
        var y = pageHeight - 50f;

        void WriteTextAt(string text, float x, float ty, float fontSize, bool bold = false)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            page.AppendLine("BT");
            page.AppendLine($"/{(bold ? "F2" : "F1")} {Fmt(fontSize)} Tf");
            page.AppendLine($"1 0 0 1 {Fmt(x)} {Fmt(ty)} Tm");
            page.AppendLine($"({EscapePdfText(Sanitize(text))}) Tj");
            page.AppendLine("ET");
        }

        void WriteTextCentered(string text, float ty, float fontSize, bool bold = false)
        {
            // Approximate centering: ~0.6 * fontSize per char
            var approxWidth = text.Length * fontSize * 0.5f;
            WriteTextAt(text, centerX - approxWidth / 2f, ty, fontSize, bold);
        }

        void WriteText(string text, float x, float fontSize, bool bold = false)
            => WriteTextAt(text, x, y, fontSize, bold);

        void HRule(float lineY, float lx = left, float rx = right, float w = 0.5f)
        {
            page.AppendLine($"{Fmt(w)} w");
            page.AppendLine($"{Fmt(lx)} {Fmt(lineY)} m");
            page.AppendLine($"{Fmt(rx)} {Fmt(lineY)} l S");
        }

        void Rect(float rx, float ry, float rw, float rh, float w = 0.5f, bool fill = false)
        {
            page.AppendLine($"{Fmt(w)} w");
            if (fill)
            {
                page.AppendLine("0.97 0.97 0.97 rg");
                page.AppendLine($"{Fmt(rx)} {Fmt(ry)} {Fmt(rw)} {Fmt(rh)} re f");
                page.AppendLine("0 0 0 rg");
            }
            page.AppendLine($"{Fmt(rx)} {Fmt(ry)} {Fmt(rw)} {Fmt(rh)} re S");
        }

        void EnsureSpace(float h)
        {
            if (y - h >= bottom) return;
            page = new StringBuilder();
            pages.Add(page);
            y = pageHeight - 50f;
        }

        var dateStr = savedCount.SavedAt.ToLocalTime().ToString("d", _text.CurrentCulture);
        var receiptNo = $"{savedCount.SavedAt.ToLocalTime():yyyyMMdd}-{savedCount.Id[..4].ToUpperInvariant()}";
        var countName = string.IsNullOrWhiteSpace(savedCount.Name) ? T("receipt.fallbackName") : savedCount.Name.Trim();

        // ── 1. HEADER ────────────────────────────────────────────────────────
        WriteTextCentered(T("receipt.title"), y, 26f, true);
        y -= 14f;
        WriteTextCentered(countName, y, 11f);
        y -= 8f;
        HRule(y, left, right, 1.5f);
        y -= 16f;

        // ── 2. DATE / RECEIPT NO ─────────────────────────────────────────────
        WriteText($"{T("receipt.date")}: {dateStr}", left, 10f);
        WriteTextAt($"{T("receipt.number")}: {receiptNo}", right - 160f, y, 10f);
        y -= 20f;

        // ── 3. AMOUNT BOX ────────────────────────────────────────────────────
        const float boxH = 52f;
        var boxTop = y;
        var boxBot = boxTop - boxH;
        Rect(left, boxBot, contentWidth, boxH, 1f, fill: true);

        WriteTextAt(T("receipt.amountReceived"), left + 14f, boxTop - 16f, 10f);
        var amountText = Money(savedCount.TotalAmount);
        // Place amount near center-right
        var approxAmountWidth = amountText.Length * 14f * 0.5f;
        WriteTextAt(amountText, centerX - approxAmountWidth / 2f + 40f, boxBot + boxH / 2f - 7f, 20f, true);
        y = boxBot - 14f;

        // ── 4. AMOUNT IN WORDS ───────────────────────────────────────────────
        EnsureSpace(20f);
        var words = isGerman ? AmountInWords(savedCount.TotalAmount) : AmountInWordsEnglish(savedCount.TotalAmount);
        WriteText(T("receipt.inWords"), left, 9f, true);
        WriteTextAt(words, left + 65f, y, 9f);
        y -= 16f;

        // ── 5. PAYMENT METHOD ────────────────────────────────────────────────
        EnsureSpace(16f);
        WriteText(T("receipt.paymentMethod"), left, 9f, true);
        WriteTextAt(T("receipt.cash"), left + 65f, y, 9f);
        y -= 14f;
        HRule(y);
        y -= 14f;

        // ── 6. DENOMINATION TABLE ────────────────────────────────────────────
        EnsureSpace(20f);
        WriteText(T("receipt.breakdown"), left, 10f, true);
        y -= 14f;

        const float rowTop = 11f;
        const float rowBot = 4f;

        var banknotes = savedCount.Denominations.Where(d => !d.IsCoin).OrderByDescending(d => d.Value).ToList();
        var coins = savedCount.Denominations.Where(d => d.IsCoin).OrderByDescending(d => d.Value).ToList();

        void WriteSection(string title, IEnumerable<DenominationCount> denoms)
        {
            var list = denoms.ToList();
            if (list.Count == 0) return;
            EnsureSpace(rowTop + rowBot + 2f);
            WriteText(title, left, 9f, true);
            y -= rowTop + rowBot;
            foreach (var denom in list)
            {
                EnsureSpace(rowTop + rowBot);
                y -= rowTop;
                WriteText(denom.DisplayName, left + 12f, 9f);
                WriteTextAt($"x {denom.Quantity}", left + 220f, y, 9f);
                WriteTextAt(Money(denom.Value * denom.Quantity), right - 90f, y, 9f);
                y -= rowBot;
                HRule(y, left + 12f, right, 0.25f);
            }
            y -= 6f;
        }

        if (banknotes.Count > 0 || coins.Count > 0)
        {
            WriteSection(T("receipt.banknotes"), banknotes);
            WriteSection(T("receipt.coins"), coins);
        }
        else
        {
            EnsureSpace(rowTop + rowBot);
            y -= rowTop;
            WriteText(T("receipt.noDenominations"), left + 12f, 9f);
            y -= rowBot;
        }

        // ── 7. TOTAL LINE ────────────────────────────────────────────────────
        EnsureSpace(rowTop + rowBot + 4f);
        HRule(y, left, right, 0.75f);
        y -= rowTop;
        WriteText(T("receipt.total"), left, 10f, true);
        WriteTextAt(Money(savedCount.TotalAmount), right - 90f, y, 10f, true);
        y -= rowBot;
        HRule(y, left, right, 1f);
        y -= 18f;

        // ── 8. SIGNATURES ────────────────────────────────────────────────────
        EnsureSpace(16f);
        WriteText(T("receipt.signatures"), left, 10f, true);
        y -= 6f;
        HRule(y, left, right, 0.75f);
        y -= 8f;

        void RenderSigBox(SavedCountSignature sig, string fallbackLabel)
        {
            const float sigBoxH = 72f;
            EnsureSpace(sigBoxH + 4f);
            var bBot = y - sigBoxH;

            page.AppendLine("0.5 w");
            page.AppendLine($"{Fmt(left)} {Fmt(bBot)} {Fmt(contentWidth)} {Fmt(sigBoxH)} re S");

            if (sig.HasAnySignature)
            {
                var signer = string.IsNullOrWhiteSpace(sig.SignerName) ? T("receipt.signer") : sig.SignerName.Trim();
                var signedOn = sig.SignedAt.HasValue
                    ? sig.SignedAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                    : T("receipt.signed");
                WriteTextAt(signer, left + 10f, bBot + sigBoxH - 16f, 10f, true);
                WriteTextAt(signedOn, left + 10f, bBot + sigBoxH - 28f, 8.5f);
            }
            else
            {
                WriteTextAt(fallbackLabel, left + 10f, bBot + sigBoxH - 16f, 8.5f);
            }

            var divX = left + contentWidth * 0.38f;
            page.AppendLine("0.4 w");
            page.AppendLine($"{Fmt(divX)} {Fmt(bBot + 8f)} m {Fmt(divX)} {Fmt(bBot + sigBoxH - 8f)} l S");

            var sigLeft = divX + 10f;
            var sigWidth = right - sigLeft - 10f;
            var sigBase = bBot + 14f;

            page.AppendLine("0.4 w");
            page.AppendLine($"{Fmt(sigLeft)} {Fmt(sigBase)} m {Fmt(right - 10f)} {Fmt(sigBase)} l S");

            if (sig.HasDrawnSignature)
                RenderDrawnSignature(page, sig.DrawnStrokes, sigLeft, sigBase + 2f, sigWidth, sigBoxH - 24f);
            else if (sig.HasTypedSignature)
                WriteTextAt(sig.TypedSignature, sigLeft + 4f, sigBase + 14f, 16f);

            y = bBot - 10f;
        }

        RenderSigBox(savedCount.Signature ?? new SavedCountSignature(), T("receipt.signer1Unsigned"));
        RenderSigBox(savedCount.SecondSignature ?? new SavedCountSignature(), T("receipt.signer2Unsigned"));

        // ── 9. FOOTER ────────────────────────────────────────────────────────
        var footerTs = savedCount.SavedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        foreach (var pg in pages)
        {
            pg.AppendLine("0.6 0.6 0.6 RG");
            pg.AppendLine($"0.4 w {Fmt(left)} 34 m {Fmt(right)} 34 l S");
            pg.AppendLine("0 0 0 RG");
            pg.AppendLine("0.5 0.5 0.5 rg");
            pg.AppendLine($"BT /F1 7.5 Tf 1 0 0 1 {Fmt(left)} 22 Tm ({EscapePdfText(Sanitize(T("receipt.generatedWith")))}) Tj ET");
            pg.AppendLine($"BT /F1 7.5 Tf 1 0 0 1 {Fmt(right - 80f)} 22 Tm ({EscapePdfText(Sanitize(footerTs))}) Tj ET");
            pg.AppendLine("0 0 0 rg");
        }

        return SimplePdfDocument.Create(pages);
    }

    // ── German amount-in-words ────────────────────────────────────────────────

    private static string AmountInWords(decimal amount)
    {
        var euros = (long)Math.Floor(amount);
        var cents = (int)Math.Round((amount - euros) * 100);
        var result = IntegerToGerman(euros) + (euros == 1 ? " Euro" : " Euro");
        if (cents > 0)
            result += " und " + IntegerToGerman(cents) + " Cent";
        if (result.Length == 0) return "Null Euro";
        return char.ToUpperInvariant(result[0]) + result[1..];
    }

    private static readonly string[] Ones =
    {
        "", "ein", "zwei", "drei", "vier", "fünf", "sechs", "sieben", "acht", "neun",
        "zehn", "elf", "zwölf", "dreizehn", "vierzehn", "fünfzehn", "sechzehn",
        "siebzehn", "achtzehn", "neunzehn"
    };

    private static readonly string[] Tens =
    {
        "", "zehn", "zwanzig", "dreißig", "vierzig", "fünfzig",
        "sechzig", "siebzig", "achtzig", "neunzig"
    };

    private static string IntegerToGerman(long n)
    {
        if (n == 0) return "null";
        if (n < 0) return "minus " + IntegerToGerman(-n);
        if (n < 20) return Ones[n];
        if (n < 100)
        {
            var t = (int)(n / 10);
            var o = (int)(n % 10);
            return o == 0 ? Tens[t] : Ones[o] + "und" + Tens[t];
        }
        if (n < 1000)
        {
            var h = (int)(n / 100);
            var rest = n % 100;
            return (h == 1 ? "ein" : Ones[h]) + "hundert" + (rest == 0 ? "" : IntegerToGerman(rest));
        }
        if (n < 1_000_000)
        {
            var k = n / 1000;
            var rest = n % 1000;
            return (k == 1 ? "ein" : IntegerToGerman(k)) + "tausend" + (rest == 0 ? "" : IntegerToGerman(rest));
        }

        var m = n / 1_000_000;
        var restM = n % 1_000_000;
        var millionStr = m == 1 ? "eine Million" : IntegerToGerman(m) + " Millionen";
        return millionStr + (restM == 0 ? "" : " " + IntegerToGerman(restM));
    }

    private static string AmountInWordsEnglish(decimal amount)
    {
        var units = (long)Math.Floor(amount);
        var cents = (int)Math.Round((amount - units) * 100);
        var result = IntegerToEnglish(units) + (units == 1 ? " dollar" : " dollars");
        if (cents > 0)
            result += " and " + IntegerToEnglish(cents) + (cents == 1 ? " cent" : " cents");
        if (result.Length == 0) return "Zero dollars";
        return char.ToUpperInvariant(result[0]) + result[1..];
    }

    private static readonly string[] OnesEn =
    {
        "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen",
        "seventeen", "eighteen", "nineteen"
    };

    private static readonly string[] TensEn =
    {
        "", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
    };

    private static string IntegerToEnglish(long n)
    {
        if (n == 0) return "zero";
        if (n < 0) return "minus " + IntegerToEnglish(-n);
        if (n < 20) return OnesEn[n];
        if (n < 100)
        {
            var t = (int)(n / 10);
            var o = (int)(n % 10);
            return o == 0 ? TensEn[t] : TensEn[t] + "-" + OnesEn[o];
        }
        if (n < 1000)
        {
            var h = (int)(n / 100);
            var rest = n % 100;
            return OnesEn[h] + " hundred" + (rest == 0 ? "" : " " + IntegerToEnglish(rest));
        }
        if (n < 1_000_000)
        {
            var k = n / 1000;
            var rest = n % 1000;
            return IntegerToEnglish(k) + " thousand" + (rest == 0 ? "" : " " + IntegerToEnglish(rest));
        }
        var m = n / 1_000_000;
        var restM = n % 1_000_000;
        return IntegerToEnglish(m) + (m == 1 ? " million" : " million") + (restM == 0 ? "" : " " + IntegerToEnglish(restM));
    }

    // ── Shared PDF helpers ────────────────────────────────────────────────────

    private static void RenderDrawnSignature(StringBuilder pg, List<SignatureStroke> strokes, float left, float bottom, float width, float height)
    {
        const double sourceWidth = 560d;
        const double sourceHeight = 180d;
        var scaleX = width / sourceWidth;
        var scaleY = height / sourceHeight;

        pg.AppendLine("0 0 0 RG");
        pg.AppendLine("1.2 w");

        foreach (var stroke in strokes.Where(s => s.Points.Count > 0))
        {
            var first = stroke.Points[0];
            pg.AppendLine($"{Fmt(left + (float)(first.X * scaleX))} {Fmt(bottom + height - (float)(first.Y * scaleY))} m");
            foreach (var point in stroke.Points.Skip(1))
                pg.AppendLine($"{Fmt(left + (float)(point.X * scaleX))} {Fmt(bottom + height - (float)(point.Y * scaleY))} l");
            pg.AppendLine("S");
        }
    }

    private static string Sanitize(string value)
    {
        var sb = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            if (ch == '\u20AC') sb.Append("EUR");
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
            WriteObject(2, $"<< /Type /Pages /Count {pages.Count} /Kids [{string.Join(' ', pageObjectNumbers.Select(n => $"{n} 0 R"))}] >>");
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
                writer.Write($"{offsets[i]:D10} 00000 n \n");
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
