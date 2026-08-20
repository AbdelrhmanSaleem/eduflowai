using System;
using EduFlowAI.AI.Infrastructure.Persistence;

namespace EduFlowAI.AI.Tests;

public class EmbeddingDimensionGuardTests
{
    [Theory]
    [InlineData("vector(1536)", 1536)]
    [InlineData("vector(768)", 768)]
    [InlineData("vector(3072)", 3072)]
    public void ParseDimension_reads_the_width(string formatType, int expected)
    {
        Assert.Equal(expected, EmbeddingDimensionGuard.ParseDimension(formatType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("vector")]          // no modifier
    [InlineData("numeric")]         // not a vector column
    [InlineData("vector(abc)")]     // unparseable width
    public void ParseDimension_returns_null_when_it_cannot_tell(string? formatType)
    {
        Assert.Null(EmbeddingDimensionGuard.ParseDimension(formatType));
    }

    [Fact]
    public void EnsureMatch_is_silent_when_the_dimensions_agree()
    {
        EmbeddingDimensionGuard.EnsureMatch(actual: 1536, expected: 1536);
    }

    [Fact]
    public void EnsureMatch_throws_on_a_proven_mismatch()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => EmbeddingDimensionGuard.EnsureMatch(actual: 768, expected: 1536));

        // The message must name both widths so the operator knows what to migrate.
        Assert.Contains("768", ex.Message);
        Assert.Contains("1536", ex.Message);
    }
}
