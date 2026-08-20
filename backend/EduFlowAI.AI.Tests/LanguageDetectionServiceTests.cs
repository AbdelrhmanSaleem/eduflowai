using EduFlowAI.AI.Application.Services;

namespace EduFlowAI.AI.Tests;

public class LanguageDetectionServiceTests
{
    [Theory]
    [InlineData("What are the admission requirements?", "en")]
    [InlineData("How do I apply for the 9-month program?", "en")]
    [InlineData("ما هي متطلبات القبول؟", "ar")]
    [InlineData("ما هي حالة طلبي في النظام؟", "ar")]
    public void DetectLanguage_ClearlySingleLanguage_ReturnsThatLanguage(string input, string expected)
    {
        Assert.Equal(expected, LanguageDetectionService.DetectLanguage(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345 67890")]      // digits/punctuation only, no letters
    [InlineData("!!! ??? ...")]
    public void DetectLanguage_NoLetters_DefaultsToEnglish(string input)
    {
        Assert.Equal("en", LanguageDetectionService.DetectLanguage(input));
    }

    [Fact]
    public void DetectLanguage_EqualCounts_TieResolvesToEnglish()
    {
        // "Hello" = 5 Latin letters, "مرحبا" = 5 Arabic letters.
        // The service returns "ar" only when arabicCount > englishCount, so a tie yields "en".
        Assert.Equal("en", LanguageDetectionService.DetectLanguage("Hello مرحبا"));
    }

    [Fact]
    public void DetectLanguage_MostlyArabicWithSomeLatin_ReturnsArabic()
    {
        Assert.Equal("ar", LanguageDetectionService.DetectLanguage("متطلبات القبول في ITI"));
    }
}
