using System;
using System.IO;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace EduFlowAI.AI.Infrastructure.ExternalServices;

// Offline fallback used when AI transcription is unavailable or the file is too large.
public class PdfProcessingService
{
    public string ExtractRawText(Stream pdfStream)
    {
        if (pdfStream == null)
            throw new ArgumentException("The provided PDF file stream is null.", nameof(pdfStream));

        var textBuilder = new StringBuilder();

        if (pdfStream.CanSeek)
            pdfStream.Position = 0;

        using var pdfReader = new PdfReader(pdfStream);
        using var pdfDocument = new PdfDocument(pdfReader);

        var pageCount = pdfDocument.GetNumberOfPages();

        for (var pageNum = 1; pageNum <= pageCount; pageNum++)
        {
            var page = pdfDocument.GetPage(pageNum);
            var pageText = PdfTextExtractor.GetTextFromPage(page);

            if (!string.IsNullOrWhiteSpace(pageText))
            {
                textBuilder.AppendLine(pageText);
                textBuilder.AppendLine();
            }
        }

        return textBuilder.ToString();
    }
}
