using System.Text;
using EduFlowAI.AI.Infrastructure.ExternalServices;

namespace EduFlowAI.AI.Tests;

public class PdfFileValidationTests
{
    [Fact]
    public void HasPdfHeader_RealPdfMagic_ReturnsTrue()
    {
        // "%PDF-1.7"
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\n...");
        Assert.True(PdfFileValidation.HasPdfHeader(bytes));
    }

    [Fact]
    public void HasPdfHeader_NonPdf_ReturnsFalse()
    {
        var bytes = Encoding.ASCII.GetBytes("PK"); // a ZIP/docx header, not a PDF
        Assert.False(PdfFileValidation.HasPdfHeader(bytes));
    }

    [Fact]
    public void HasPdfHeader_TooShort_ReturnsFalse()
    {
        Assert.False(PdfFileValidation.HasPdfHeader(new byte[] { 0x25, 0x50 }));
        Assert.False(PdfFileValidation.HasPdfHeader(System.Array.Empty<byte>()));
    }

    [Fact]
    public async Task HasPdfHeaderAsync_ResetsStreamPosition()
    {
        using var stream = new System.IO.MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.4 body"));
        stream.Position = 5;

        var ok = await PdfFileValidation.HasPdfHeaderAsync(stream);

        Assert.True(ok);
        Assert.Equal(0, stream.Position); // rewound so the caller can re-read from the start
    }
}
