using EduFlowAI.AI.Application.Services;

namespace EduFlowAI.AI.Tests;

public class InputSanitizerServiceTests
{
    [Fact]
    public void FourteenDigitNationalId_IsMasked()
    {
        var result = InputSanitizerService.MaskSensitiveNumbers("My national id is 30201011234567 please check");

        Assert.DoesNotContain("30201011234567", result);
        Assert.Contains(InputSanitizerService.Mask, result);
    }

    [Fact]
    public void ArabicIndicDigits_AreAlsoMasked()
    {
        // ٣٠٢٠١٠١١٢٣٤٥٦٧ — 14 Arabic-Indic digits
        var text = "رقمي القومي ٣٠٢٠١٠١١٢٣٤٥٦٧";

        var result = InputSanitizerService.MaskSensitiveNumbers(text);

        Assert.DoesNotContain("٣٠٢٠١٠١١٢٣٤٥٦٧", result);
        Assert.Contains(InputSanitizerService.Mask, result);
    }

    [Theory]
    [InlineData("I graduated in 2024 with grade Good")]        // year
    [InlineData("The deadline is 30 September")]               // small numbers
    [InlineData("My phone is 01234567890")]                    // 11 digits - below threshold
    public void ShortNumbers_AreLeftAlone(string text)
    {
        Assert.Equal(text, InputSanitizerService.MaskSensitiveNumbers(text));
    }

    [Fact]
    public void TwelveDigits_IsTheThreshold()
    {
        Assert.Contains(InputSanitizerService.Mask, InputSanitizerService.MaskSensitiveNumbers("id 123456789012"));   // 12 -> masked
        Assert.DoesNotContain(InputSanitizerService.Mask, InputSanitizerService.MaskSensitiveNumbers("id 12345678901")); // 11 -> kept
    }

    [Fact]
    public void MultipleLongNumbers_AreAllMasked()
    {
        var result = InputSanitizerService.MaskSensitiveNumbers("30201011234567 and 29805051234567");

        Assert.DoesNotContain("30201011234567", result);
        Assert.DoesNotContain("29805051234567", result);
    }

    [Fact]
    public void TextWithoutDigits_IsUnchanged()
    {
        const string text = "What are the admission requirements?";
        Assert.Equal(text, InputSanitizerService.MaskSensitiveNumbers(text));
    }

    [Fact]
    public void NullOrEmpty_IsSafe()
    {
        Assert.Equal(string.Empty, InputSanitizerService.MaskSensitiveNumbers(null));
        Assert.Equal(string.Empty, InputSanitizerService.MaskSensitiveNumbers(""));
    }
}
