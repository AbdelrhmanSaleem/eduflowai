using System.Text.RegularExpressions;

namespace EduFlowAI.AI.Application.Services;

// Defensive scrub before user text leaves for the model. \d also covers Arabic-Indic digits.
public static class InputSanitizerService
{
    public const string Mask = "[hidden]";

    private static readonly Regex LongDigitRun = new(@"\d{12,}", RegexOptions.Compiled);

    public static string MaskSensitiveNumbers(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        return LongDigitRun.Replace(text, Mask);
    }
}
