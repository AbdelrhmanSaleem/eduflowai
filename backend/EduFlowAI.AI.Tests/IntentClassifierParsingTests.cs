using EduFlowAI.AI.Application.Services;

namespace EduFlowAI.AI.Tests;

public class IntentClassifierParsingTests
{
    [Fact]
    public void PlainCamelCaseJson_ParsesStatusIntent()
    {
        var raw = "{\"primaryIntent\":\"application_status\",\"confidence\":0.9,\"requiresClarification\":false}";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("application_status", dto.PrimaryIntent);
        Assert.Equal("application_status_service", dto.RoutedTo);
        Assert.Equal(0.9m, dto.Confidence);
    }

    [Fact]
    public void RecommendationIntent_RoutesToRecommendationsAgent()
    {
        var raw = "{\"primaryIntent\":\"recommendation\",\"confidence\":0.8}";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("recommendations_agent", dto.RoutedTo);
    }

    [Fact]
    public void FencedJsonBlock_IsParsed()
    {
        var raw = "```json\n{\"primaryIntent\":\"knowledge\",\"confidence\":0.95}\n```";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("knowledge", dto.PrimaryIntent);
        Assert.Equal("knowledge_rag", dto.RoutedTo);
    }

    [Fact]
    public void FencedBlockWithoutLanguage_IsParsed()
    {
        var raw = "```\n{\"primaryIntent\":\"application_status\",\"confidence\":0.7}\n```";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("application_status", dto.PrimaryIntent);
    }

    [Fact]
    public void LeadingProseThenJson_IsParsed()
    {
        var raw = "Sure! Here is the classification:\n{\"primaryIntent\":\"recommendation\",\"confidence\":0.6}";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("recommendation", dto.PrimaryIntent);
    }

    [Fact]
    public void UppercaseIntent_IsNormalized()
    {
        var raw = "{\"primaryIntent\":\"APPLICATION_STATUS\",\"confidence\":0.9}";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("application_status", dto.PrimaryIntent);
        Assert.Equal("application_status_service", dto.RoutedTo);
    }

    [Fact]
    public void ConfidenceAboveOne_IsClampedToOne()
    {
        var raw = "{\"primaryIntent\":\"knowledge\",\"confidence\":1.5}";

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Equal(1m, dto.Confidence);
    }

    [Fact]
    public void NegativeConfidence_IsClampedToZero()
    {
        var raw = "{\"primaryIntent\":\"knowledge\",\"confidence\":-0.4}";

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Equal(0m, dto.Confidence);
    }

    [Theory]
    [InlineData("this is not json at all")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{\"primaryIntent\":\"weather\",\"confidence\":0.9}")]  // unknown intent value
    [InlineData("{\"confidence\":0.9}")]                                 // missing intent
    [InlineData("{ broken json ")]                                      // malformed
    public void UnparseableOrInvalid_ReturnsFalseAndKnowledgeFallback(string raw)
    {
        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.False(ok);
        Assert.Equal("knowledge", dto.PrimaryIntent);
        Assert.Equal("knowledge_rag", dto.RoutedTo);

        // A parse failure is our defect, not the user's ambiguity: answer from the knowledge base
        // rather than pushing them into a clarification loop. Clarification is reserved for a
        // successful classification whose confidence is below the configured threshold.
        Assert.False(dto.RequiresClarification);
    }

    [Fact]
    public void NullInput_ReturnsFalseAndFallback()
    {
        var ok = IntentClassifierService.TryParseIntentResponse(null, out var dto);

        Assert.False(ok);
        Assert.Equal("knowledge", dto.PrimaryIntent);
    }

    // The router writes the query retrieval embeds, keeping greeting text out of the vector.
    [Fact]
    public void SearchQuery_FromTheModel_IsParsed()
    {
        var raw = "{\"intents\":[{\"intent\":\"knowledge\",\"confidence\":0.9}],\"language\":\"ar\"," +
                  "\"searchQuery\":\"tracks offered at Alexandria branch\"}";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("tracks offered at Alexandria branch", dto.SearchQuery);
    }

    [Fact]
    public void SearchQuery_IsTrimmed()
    {
        var raw = "{\"primaryIntent\":\"knowledge\",\"confidence\":0.9,\"searchQuery\":\"  admission dates  \"}";

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Equal("admission dates", dto.SearchQuery);
    }

    // Blank means no lookup is needed; an over-long value is the model rambling.
    [Fact]
    public void BlankOrMissingSearchQuery_IsNull()
    {
        IntentClassifierService.TryParseIntentResponse(
            "{\"primaryIntent\":\"knowledge\",\"confidence\":0.9,\"searchQuery\":\"   \"}", out var blank);
        IntentClassifierService.TryParseIntentResponse(
            "{\"primaryIntent\":\"knowledge\",\"confidence\":0.9}", out var missing);

        Assert.Null(blank.SearchQuery);
        Assert.Null(missing.SearchQuery);
    }

    [Fact]
    public void OverlongSearchQuery_IsDropped()
    {
        var overlong = new string('a', 301);
        var raw = "{\"primaryIntent\":\"knowledge\",\"confidence\":0.9,\"searchQuery\":\"" + overlong + "\"}";

        IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.Null(dto.SearchQuery);
    }

    [Fact]
    public void Language_FromTheModel_IsParsed()
    {
        var raw = "{\"intents\":[{\"intent\":\"knowledge\",\"confidence\":0.9}],\"language\":\"ar\"}";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("ar", dto.Language);
    }

    [Fact]
    public void Language_IsNormalizedToLowercase()
    {
        var raw = "{\"primaryIntent\":\"knowledge\",\"confidence\":0.9,\"language\":\"EN\"}";

        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Equal("en", dto.Language);
    }

    [Theory]
    [InlineData("{\"primaryIntent\":\"knowledge\",\"confidence\":0.9}")]                     // no language field
    [InlineData("{\"primaryIntent\":\"knowledge\",\"confidence\":0.9,\"language\":\"fr\"}")]  // unsupported value
    [InlineData("{\"primaryIntent\":\"knowledge\",\"confidence\":0.9,\"language\":\"\"}")]    // empty
    public void UnusableLanguage_IsNull_SoTheCallerCanFallBack(string raw)
    {
        var ok = IntentClassifierService.TryParseIntentResponse(raw, out var dto);

        Assert.True(ok);
        Assert.Null(dto.Language);
    }
}
