using System.Text.RegularExpressions;

namespace EduFlowAI.AI.Application.Services;

public class LanguageDetectionService
{
    public static string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "en";  // Default to English

        // Arabic Unicode range: U+0600 to U+06FF
        var arabicPattern = new Regex(@"[\u0600-\u06FF]");
        var englishPattern = new Regex(@"[a-zA-Z]");

        var arabicCount = arabicPattern.Matches(text).Count;
        var englishCount = englishPattern.Matches(text).Count;

        return arabicCount > englishCount ? "ar" : "en";
    }
}