using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using System.Text.RegularExpressions;

namespace AIChatAssistant.Services.AI.RAGPipeline
{
    public class Reranker : IReranker
    {
        private static readonly HashSet<string> StopWords =
    new(StringComparer.OrdinalIgnoreCase)
    {
            "a",
            "an",
            "and",
            "are",
            "as",
            "at",
            "be",
            "by",
            "does",
            "for",
            "from",
            "how",
            "in",
            "is",
            "it",
            "of",
            "on",
            "or",
            "that",
            "the",
            "this",
            "to",
            "was",
            "what",
            "when",
            "where",
            "which",
            "who",
            "why",
            "with",
            "use"
    };
        public Task<List<SearchResult>> RerankAsync(
      string question,
      IReadOnlyList<SearchResult> candidates,
      int topK)
        {
            var keywords =
                question.ToLowerInvariant()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Select(word =>
                        word.Trim(
                            '.', ',', '!', '?', ':', ';',
                            '"', '\'', '(', ')', '[', ']',
                            '{', '}', '<', '>', '/'))
                    .Where(word =>
                        !string.IsNullOrWhiteSpace(word) &&
                        !StopWords.Contains(word))
                    .Distinct()
                    .ToList();

            const float semanticWeight = 0.7f;
            const float keywordWeight = 0.3f;

            var reranked = candidates
                .Select(result =>
                {
                    var keywordScore =
                        CalculateKeywordScore(
                            result.Record.Content,
                            keywords);

                    var semanticScore =
                        Math.Clamp(
                            result.Similarity,
                            0f,
                            1f);

                    var combinedScore =
                        (semanticScore * semanticWeight) +
                        (keywordScore * keywordWeight);

                    Console.WriteLine(
                        $"Semantic={semanticScore:F3}, " +
                        $"Keyword={keywordScore:F3}, " +
                        $"Combined={combinedScore:F3}");

                    return new
                    {
                        Result = result,
                        Score = combinedScore
                    };
                })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select((x, index) =>
                {
                    x.Result.Rank = index + 1;
                    x.Result.RerankScore = x.Score;

                    return x.Result;
                })
                .ToList();

            return Task.FromResult(reranked);
        }
        private static float CalculateKeywordScore(
    string content,
    IReadOnlyList<string> keywords)
        {
            if (string.IsNullOrWhiteSpace(content) ||
                keywords.Count == 0)
            {
                return 0f;
            }

            var contentWords =
                Regex.Split(
                    content.ToLowerInvariant(),
                    @"\s+")
                .Select(word =>
                    word.Trim(
                        '.', ',', '!', '?', ':', ';',
                        '"', '\'', '(', ')', '[', ']',
                        '{', '}', '<', '>', '/'))
                .Where(word =>
                    !string.IsNullOrWhiteSpace(word))
                .ToList();

            if (contentWords.Count == 0)
            {
                return 0f;
            }

            var contentWordSet =
                contentWords.ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

            var matchedKeywords =
                keywords.Count(keyword =>
                    contentWordSet.Contains(keyword));

            var coverageScore =
                (float)matchedKeywords / keywords.Count;

            var keywordOccurrences =
                keywords.Sum(keyword =>
                    contentWords.Count(word =>
                        string.Equals(
                            word,
                            keyword,
                            StringComparison.OrdinalIgnoreCase)));

            var keywordDensity =
                (float)keywordOccurrences /
                contentWords.Count;

            // Density is useful for distinguishing
            // focused content from large documents where
            // the keyword appears only incidentally.
            var densityScore =
                Math.Clamp(
                    keywordDensity * 10f,
                    0f,
                    1f);

            var score =
                (coverageScore * 0.7f) +
                (densityScore * 0.3f);

            Console.WriteLine(
                $"Keyword coverage: {coverageScore:F3}, " +
                $"Density: {densityScore:F3}, " +
                $"Keyword score: {score:F3}");

            Console.WriteLine(
                $"Content words: {contentWords.Count}, " +
                $"Keyword occurrences: {keywordOccurrences}");

            return score;
        }
        private static float CalculateKeywordScore1(
        string content,
        IReadOnlyList<string> keywords)
        {
            if (string.IsNullOrWhiteSpace(content) ||
                keywords.Count == 0)
            {
                return 0;
            }

            var normalizedContent =
                content.ToLowerInvariant();

            var score = 0;

            foreach (var keyword in keywords)
            {
                if (normalizedContent.Contains(
                        keyword,
                        StringComparison.Ordinal))
                {
                    score++;
                }
            }

            return (float)score / keywords.Count;
        }
    }
}
