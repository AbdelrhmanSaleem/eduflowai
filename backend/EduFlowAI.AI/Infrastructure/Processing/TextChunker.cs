using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EduFlowAI.AI.Infrastructure.Processing;

public static class TextChunker
{
    private static readonly Regex SentenceBoundary = new(@"(?<=[\.!\?؟])\s+", RegexOptions.Compiled);
    private static readonly Regex BlockBoundary = new(@"\n\s*\n", RegexOptions.Compiled);

    private const int MinUsefulChars = 40;

    public static List<string> Split(string? text, int maxChars = 1200, int overlapChars = 200)
    {
        var chunks = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        if (maxChars <= 0)
            maxChars = 1200;

        if (overlapChars < 0)
            overlapChars = 0;

        // Keep the overlap well under the window or the carried tail leaves no room for the next block.
        overlapChars = Math.Min(overlapChars, maxChars / 2);

        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");

        var blocks = BlockBoundary.Split(normalized)
            .Select(b => b.Trim())
            .Where(b => b.Length > 0)
            .ToList();

        var current = new StringBuilder();

        foreach (var block in blocks)
        {
            if (block.Length > maxChars)
            {
                Flush(chunks, current);
                foreach (var piece in SplitOversizedBlock(block, maxChars))
                    chunks.Add(piece);
                continue;
            }

            if (current.Length > 0 && current.Length + 1 + block.Length > maxChars)
            {
                var completed = current.ToString().Trim();
                chunks.Add(completed);

                current.Clear();

                var tail = TakeTail(completed, overlapChars);
                if (tail.Length > 0 && tail.Length + 1 + block.Length <= maxChars)
                    current.Append(tail).Append('\n');
            }

            if (current.Length > 0)
                current.Append('\n');
            current.Append(block);
        }

        Flush(chunks, current);

        return MergeTinyFragments(chunks.Where(c => c.Trim().Length > 0).ToList(), maxChars);
    }

    private static void Flush(List<string> chunks, StringBuilder buffer)
    {
        if (buffer.Length == 0)
            return;

        var value = buffer.ToString().Trim();
        if (value.Length > 0)
            chunks.Add(value);

        buffer.Clear();
    }

    private static IEnumerable<string> SplitOversizedBlock(string block, int maxChars)
    {
        var buffer = new StringBuilder();

        foreach (var sentence in SentenceBoundary.Split(block).Where(s => s.Trim().Length > 0))
        {
            var trimmed = sentence.Trim();

            if (trimmed.Length > maxChars)
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString().Trim();
                    buffer.Clear();
                }

                foreach (var wordChunk in SplitOnWords(trimmed, maxChars))
                    yield return wordChunk;

                continue;
            }

            if (buffer.Length > 0 && buffer.Length + 1 + trimmed.Length > maxChars)
            {
                yield return buffer.ToString().Trim();
                buffer.Clear();
            }

            if (buffer.Length > 0)
                buffer.Append(' ');
            buffer.Append(trimmed);
        }

        if (buffer.Length > 0)
            yield return buffer.ToString().Trim();
    }

    private static IEnumerable<string> SplitOnWords(string sentence, int maxChars)
    {
        var buffer = new StringBuilder();

        foreach (var word in sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            // Only a single token longer than the window is ever cut.
            if (word.Length > maxChars)
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString().Trim();
                    buffer.Clear();
                }

                for (var i = 0; i < word.Length; i += maxChars)
                    yield return word.Substring(i, Math.Min(maxChars, word.Length - i));

                continue;
            }

            if (buffer.Length > 0 && buffer.Length + 1 + word.Length > maxChars)
            {
                yield return buffer.ToString().Trim();
                buffer.Clear();
            }

            if (buffer.Length > 0)
                buffer.Append(' ');
            buffer.Append(word);
        }

        if (buffer.Length > 0)
            yield return buffer.ToString().Trim();
    }

    private static string TakeTail(string text, int overlapChars)
    {
        if (overlapChars <= 0)
            return string.Empty;

        if (text.Length <= overlapChars)
            return text.Trim();

        var tail = text.Substring(text.Length - overlapChars);

        var firstSpace = tail.IndexOfAny(new[] { ' ', '\n' });
        if (firstSpace >= 0)
            tail = tail.Substring(firstSpace + 1);

        return tail.Trim();
    }

    // A split wide table can leave a remnant like "23% | |" that costs an embedding but carries nothing - fold it into a neighbour.
    private static List<string> MergeTinyFragments(List<string> chunks, int maxChars)
    {
        var merged = new List<string>();

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];

            if (chunk.Trim().Length >= MinUsefulChars)
            {
                merged.Add(chunk);
                continue;
            }

            if (merged.Count > 0 && merged[^1].Length + 1 + chunk.Length <= maxChars)
            {
                merged[^1] = merged[^1] + "\n" + chunk;
                continue;
            }

            if (i + 1 < chunks.Count && chunks[i + 1].Length + 1 + chunk.Length <= maxChars)
            {
                chunks[i + 1] = chunk + "\n" + chunks[i + 1];
                continue;
            }

            merged.Add(chunk);
        }

        return merged;
    }
}
