using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EduFlowAI.AI.Application.Services;

// Pulls out the proper nouns worth matching literally, which a dense vector can rank away.
public static class SearchTermExtractor
{
    private const int MinTermLength = 4;
    private const int MaxTerms = 6;

    // Frequent enough across the knowledge base that matching them adds noise rather than signal.
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "after", "also", "and", "any", "are", "available", "before", "between", "both", "can",
        "course", "courses", "does", "each", "every", "for", "from", "have", "how", "information",
        "into", "iti", "its", "list", "more", "most", "much", "need", "offer", "offered", "offers",
        "only", "other", "over", "please", "program", "programs", "provide", "question", "requirement",
        "requirements", "should", "some", "such", "tell", "than", "that", "the", "their", "them",
        "there", "these", "they", "this", "those", "through", "track", "tracks", "under", "want",
        "what", "when", "where", "which", "who", "will", "with", "would", "you", "your"
    };

    public static IReadOnlyList<string> Extract(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<string>();

        return SplitWords(query)
            .Where(word => word.Length >= MinTermLength && !Stopwords.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTerms)
            .ToList();
    }

    // Any run of letters or digits is a word; everything else separates.
    private static IEnumerable<string> SplitWords(string query)
    {
        var word = new StringBuilder();

        foreach (var character in query)
        {
            if (char.IsLetterOrDigit(character))
            {
                word.Append(character);
                continue;
            }

            if (word.Length > 0)
            {
                yield return word.ToString();
                word.Clear();
            }
        }

        if (word.Length > 0)
            yield return word.ToString();
    }
}
