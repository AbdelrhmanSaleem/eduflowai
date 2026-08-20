using System;
using System.Collections.Generic;
using System.Linq;
using EduFlowAI.AI.Infrastructure.Processing;

namespace EduFlowAI.AI.Tests;

public class TextChunkerTests
{
    private const int Max = 200;

    [Fact]
    public void ShortText_ProducesSingleTrimmedChunk()
    {
        var chunks = TextChunker.Split("hello world", Max, 20);

        Assert.Single(chunks);
        Assert.Equal("hello world", chunks[0]);
    }

    [Fact]
    public void EmptyOrWhitespace_ProducesNoChunks()
    {
        Assert.Empty(TextChunker.Split(null, Max, 20));
        Assert.Empty(TextChunker.Split("", Max, 20));
        Assert.Empty(TextChunker.Split("   \n\n \t ", Max, 20));
    }

    [Fact]
    public void NoChunkExceedsTheLimit()
    {
        var text = string.Join("\n\n", Enumerable.Range(1, 40).Select(i => $"Paragraph {i} about the ITI admission process and its requirements."));

        var chunks = TextChunker.Split(text, Max, 20);

        Assert.All(chunks, c => Assert.True(c.Length <= Max, $"chunk of {c.Length} chars exceeded {Max}"));
    }

    /// <summary>
    /// The regression that motivated this chunker: the old fixed-width window produced fragments
    /// like "d this material." and "sive web, ES.Next". Every source word must survive intact
    /// in at least one chunk.
    /// </summary>
    [Fact]
    public void NeverSplitsAWordInHalf()
    {
        var text = string.Join(" ", Enumerable.Range(1, 300).Select(i => $"word{i}"));

        var chunks = TextChunker.Split(text, Max, 20);
        var wordsInChunks = new HashSet<string>(
            chunks.SelectMany(c => c.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)));

        foreach (var word in text.Split(' '))
            Assert.Contains(word, wordsInChunks);
    }

    [Fact]
    public void KeepsWholeParagraphsTogetherWhenTheyFit()
    {
        var a = "First paragraph about eligibility.";
        var b = "Second paragraph about the exam.";

        var chunks = TextChunker.Split($"{a}\n\n{b}", Max, 20);

        Assert.Single(chunks);
        Assert.Contains(a, chunks[0]);
        Assert.Contains(b, chunks[0]);
    }

    [Fact]
    public void ConsecutiveChunksOverlap()
    {
        var text = string.Join("\n\n", Enumerable.Range(1, 30).Select(i => $"Paragraph number {i} describing part of the program in detail."));

        var chunks = TextChunker.Split(text, Max, 60);

        Assert.True(chunks.Count > 1);

        // The tail of one chunk should reappear at the head of the next.
        var firstChunkWords = chunks[0].Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var secondChunkStart = chunks[1].Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).First();
        Assert.Contains(secondChunkStart, firstChunkWords);
    }

    [Fact]
    public void SplitsLongParagraphOnSentenceBoundaries()
    {
        var sentences = Enumerable.Range(1, 12).Select(i => $"This is sentence number {i} in a long paragraph.");
        var text = string.Join(" ", sentences);

        var chunks = TextChunker.Split(text, Max, 20);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Length <= Max));
        // Sentences stay intact rather than being cut mid-way.
        Assert.Contains(chunks, c => c.Contains("This is sentence number 1 in a long paragraph."));
    }

    [Fact]
    public void SingleTokenLongerThanLimit_DoesNotLoopForever()
    {
        var giant = new string('x', Max * 3);

        var chunks = TextChunker.Split(giant, Max, 20);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.True(c.Length <= Max));
    }

    [Fact]
    public void BadOverlap_IsCorrectedInsteadOfStalling()
    {
        var text = string.Join("\n\n", Enumerable.Range(1, 20).Select(i => $"Block {i} with some content."));

        var chunks = TextChunker.Split(text, 50, overlapChars: 500); // overlap >= max

        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.True(c.Length <= 50));
    }

    [Fact]
    public void TinyTrailingFragment_IsMergedIntoPreviousChunk()
    {
        // Splitting a wide Markdown table used to leave a remnant like "23% | |" as its own chunk:
        // no retrievable meaning, but still a wasted embedding.
        var bigBlock = string.Join(" ", Enumerable.Range(1, 30).Select(i => $"Sentence {i} of the table description."));
        var text = bigBlock + " 23%";

        var chunks = TextChunker.Split(text, Max, 20);

        Assert.All(chunks, c => Assert.True(c.Trim().Length >= 40 || chunks.Count == 1,
            $"tiny fragment left un-merged: '{c}'"));
    }

    [Fact]
    public void ArabicSentences_AreChunkedWithoutBreakingWords()
    {
        var text = string.Join(" ", Enumerable.Repeat("ما هي شروط القبول في المعهد؟", 20));

        var chunks = TextChunker.Split(text, Max, 20);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.True(c.Length <= Max));
        Assert.Contains(chunks, c => c.Contains("القبول"));
    }
}
