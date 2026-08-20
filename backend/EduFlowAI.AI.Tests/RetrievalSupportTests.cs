using System.Linq;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Infrastructure.Processing;

namespace EduFlowAI.AI.Tests;

public class SearchTermExtractorTests
{
    // The whole point of the question is the place name; "tracks"/"offered" match half the corpus.
    [Fact]
    public void Keeps_the_proper_noun_and_drops_the_common_words()
    {
        var terms = SearchTermExtractor.Extract("tracks offered at Alexandria branch");

        Assert.Contains("Alexandria", terms);
        Assert.Contains("branch", terms);
        Assert.DoesNotContain("tracks", terms);
        Assert.DoesNotContain("offered", terms);
    }

    [Fact]
    public void Punctuation_does_not_stick_to_a_term()
    {
        var terms = SearchTermExtractor.Extract("Is 'Cyber-Security' offered at Alexandria?");

        Assert.Contains("Cyber", terms);
        Assert.Contains("Security", terms);
        Assert.Contains("Alexandria", terms);
    }

    [Fact]
    public void Repeated_terms_are_listed_once()
    {
        var terms = SearchTermExtractor.Extract("Alexandria alexandria ALEXANDRIA");

        Assert.Single(terms);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("what are the")]
    public void Nothing_worth_matching_yields_no_terms(string? query)
    {
        Assert.Empty(SearchTermExtractor.Extract(query));
    }
}

public class ChunkContextualizerTests
{
    private const string Source = """
# Locations

## Ismailia
7 track(s) offered at Ismailia:
- Cloud Architecture

## Alexandria
6 track(s) offered at Alexandria:
- Systems Administration
- 2D Animation and Motion Graphics
""";

    // A chunk that lost its heading lists tracks without saying whose they are.
    [Fact]
    public void An_orphaned_list_is_stamped_with_its_document_and_section()
    {
        var orphan = "- Systems Administration\n- 2D Animation and Motion Graphics";

        var result = ChunkContextualizer.Contextualize("Locations.md", Source, new[] { orphan });

        Assert.StartsWith("[Document: Locations.md] [Section: Alexandria]", result.Single());
        Assert.Contains("Systems Administration", result.Single());
    }

    [Fact]
    public void Each_chunk_gets_the_section_it_actually_sits_under()
    {
        var chunks = new[]
        {
            "7 track(s) offered at Ismailia:",
            "6 track(s) offered at Alexandria:"
        };

        var result = ChunkContextualizer.Contextualize("Locations.md", Source, chunks);

        Assert.Contains("[Section: Ismailia]", result[0]);
        Assert.Contains("[Section: Alexandria]", result[1]);
    }

    // A chunk spanning a boundary carries its own headings, so no section is added.
    [Fact]
    public void A_chunk_with_its_own_heading_is_not_given_a_section()
    {
        var spanning = "- Cloud Architecture\n\n## Alexandria\n6 track(s) offered at Alexandria:";

        var result = ChunkContextualizer.Contextualize("Locations.md", Source, new[] { spanning }).Single();

        Assert.StartsWith("[Document: Locations.md]", result);
        Assert.DoesNotContain("[Section:", result);
    }

    [Fact]
    public void Text_that_cannot_be_located_still_keeps_the_document_name()
    {
        var result = ChunkContextualizer.Contextualize("Locations.md", Source, new[] { "not in the source" });

        Assert.StartsWith("[Document: Locations.md]", result.Single());
    }

    [Fact]
    public void No_chunks_means_no_work()
    {
        Assert.Empty(ChunkContextualizer.Contextualize("Locations.md", Source, System.Array.Empty<string>()));
    }
}
