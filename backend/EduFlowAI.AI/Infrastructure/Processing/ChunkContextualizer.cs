using System;
using System.Collections.Generic;
using System.Linq;

namespace EduFlowAI.AI.Infrastructure.Processing;

// Stamps each chunk with its document and section so a fragment split from its heading stays usable.
public static class ChunkContextualizer
{
    private const char LineFeed = '\n';

    public static List<string> Contextualize(
        string? documentName,
        string? sourceText,
        IReadOnlyList<string> chunks)
    {
        if (chunks is null || chunks.Count == 0)
            return new List<string>();

        var normalized = (sourceText ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        var headings = FindHeadings(normalized);
        var contextualized = new List<string>(chunks.Count);
        var searchFrom = 0;

        foreach (var chunk in chunks)
        {
            var position = LocateChunk(normalized, chunk, ref searchFrom);

            // Only a chunk with no heading of its own needs one supplied.
            var heading = ContainsHeading(chunk)
                ? null
                : HeadingAt(headings, position);

            contextualized.Add(BuildHeader(documentName, heading) + chunk);
        }

        return contextualized;
    }

    private static bool ContainsHeading(string chunk)
    {
        foreach (var line in chunk.Split(LineFeed))
        {
            if (line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string BuildHeader(string? documentName, string? heading)
    {
        var name = string.IsNullOrWhiteSpace(documentName) ? null : documentName.Trim();

        if (name is null && heading is null)
            return string.Empty;

        var parts = new List<string>(2);

        if (name is not null)
            parts.Add($"[Document: {name}]");

        if (heading is not null)
            parts.Add($"[Section: {heading}]");

        return string.Join(" ", parts) + LineFeed;
    }

    // Every Markdown heading with its offset, so a chunk maps to the one preceding it.
    private static List<(int Position, string Text)> FindHeadings(string text)
    {
        var headings = new List<(int, string)>();
        var offset = 0;

        foreach (var line in text.Split(LineFeed))
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                var title = trimmed.TrimStart('#').Trim();

                if (title.Length > 0)
                    headings.Add((offset, title));
            }

            offset += line.Length + 1;
        }

        return headings;
    }

    private static string? HeadingAt(List<(int Position, string Text)> headings, int position)
    {
        if (headings.Count == 0 || position < 0)
            return null;

        string? current = null;

        foreach (var heading in headings)
        {
            if (heading.Position > position)
                break;

            current = heading.Text;
        }

        return current;
    }

    // Chunks are contiguous slices; the cursor keeps repeated text on the right occurrence.
    private static int LocateChunk(string text, string chunk, ref int searchFrom)
    {
        var probe = chunk
            .Split(LineFeed)
            .FirstOrDefault(line => line.Trim().Length > 0)
            ?.Trim();

        if (string.IsNullOrEmpty(probe) || text.Length == 0)
            return -1;

        if (probe.Length > 80)
            probe = probe.Substring(0, 80);

        var start = Math.Clamp(searchFrom, 0, Math.Max(0, text.Length - 1));
        var index = text.IndexOf(probe, start, StringComparison.Ordinal);

        if (index < 0)
            index = text.IndexOf(probe, StringComparison.Ordinal);

        if (index >= 0)
            searchFrom = index + 1;

        return index;
    }
}
