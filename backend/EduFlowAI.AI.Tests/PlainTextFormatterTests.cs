using EduFlowAI.AI.Application.Services;

namespace EduFlowAI.AI.Tests;

// The chat renders replies as plain text, so Markdown reached the applicant as literal asterisks.
public class PlainTextFormatterTests
{
    [Fact]
    public void Bold_markers_are_removed_but_the_words_stay()
    {
        var result = PlainTextFormatter.Clean("We offer **AI and Machine Learning** this intake.");

        Assert.Equal("We offer AI and Machine Learning this intake.", result);
    }

    [Fact]
    public void Markdown_bullets_become_a_numbered_list()
    {
        var raw = "We offer 2 tracks:\n*   **AI and Machine Learning** (Cognitive)\n-  Systems Administration";

        var result = PlainTextFormatter.Clean(raw);

        Assert.DoesNotContain("*", result);
        Assert.Contains("1. AI and Machine Learning (Cognitive)", result);
        Assert.Contains("2. Systems Administration", result);
    }

    // A blank line can sit between items, so the numbering keeps counting across it.
    [Fact]
    public void A_blank_line_inside_a_list_does_not_restart_the_numbering()
    {
        var result = PlainTextFormatter.Clean("- First\n\n- Second\n- Third");

        Assert.Contains("1. First", result);
        Assert.Contains("2. Second", result);
        Assert.Contains("3. Third", result);
    }

    // Prose between two lists means they are separate lists, so the count starts again.
    [Fact]
    public void A_paragraph_between_lists_restarts_the_numbering()
    {
        var result = PlainTextFormatter.Clean("- Alpha\n- Beta\nThen some prose.\n- Gamma");

        Assert.Contains("1. Alpha", result);
        Assert.Contains("2. Beta", result);
        Assert.Contains("1. Gamma", result);
    }

    // The exact shape the assistant produced for "which tracks are offered at Alexandria".
    [Fact]
    public void An_arabic_reply_keeps_its_words_and_loses_its_asterisks()
    {
        var raw = "نقدم في فرع الإسكندرية 6 مسارات:\n*   **AI and Machine Learning** (الذكاء الاصطناعي)";

        var result = PlainTextFormatter.Clean(raw);

        Assert.DoesNotContain("*", result);
        Assert.Contains("نقدم في فرع الإسكندرية 6 مسارات:", result);
        Assert.Contains("1. AI and Machine Learning (الذكاء الاصطناعي)", result);
    }

    // A list the model already numbered is left exactly as it is.
    [Fact]
    public void An_already_numbered_list_is_untouched()
    {
        var raw = "Steps:\n1. Apply online\n2. Upload your documents";

        var result = PlainTextFormatter.Clean(raw);

        Assert.Equal("Steps:\n1. Apply online\n2. Upload your documents", result);
    }

    [Fact]
    public void Headings_backticks_and_italics_are_stripped()
    {
        var result = PlainTextFormatter.Clean("## Requirements\nA `Bachelor` degree is *required*.");

        Assert.Equal("Requirements\nA Bachelor degree is required.", result);
    }

    [Fact]
    public void Runs_of_blank_lines_collapse_to_one()
    {
        var result = PlainTextFormatter.Clean("First paragraph.\n\n\n\nSecond paragraph.");

        Assert.Equal("First paragraph.\n\nSecond paragraph.", result);
    }

    // A lone asterisk or an identifier is not emphasis and must survive untouched.
    [Theory]
    [InlineData("Rated 4.5 * out of 5", "Rated 4.5 * out of 5")]
    [InlineData("The field is called user_name here", "The field is called user_name here")]
    public void Text_that_only_looks_like_markdown_is_left_alone(string raw, string expected)
    {
        Assert.Equal(expected, PlainTextFormatter.Clean(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Nothing_to_clean_gives_an_empty_string(string? raw)
    {
        Assert.Equal(string.Empty, PlainTextFormatter.Clean(raw));
    }
}
