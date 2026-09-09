using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;

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
            var keywords = question.ToLowerInvariant()
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

            var reranked = candidates
                .Select(result =>
                {
                    var keywordScore =
                        CalculateKeywordScore(
                            result.Record.Content,
                            keywords);

                    return new
                    {
                        Result = result,
                        Score = keywordScore
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
                return 0;
            }

            var contentWords =
                content
                    .ToLowerInvariant()
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
                        !string.IsNullOrWhiteSpace(word))
                    .ToHashSet();

            var matchedKeywords =
                keywords.Count(keyword =>
                    contentWords.Contains(keyword));

            return (float)matchedKeywords / keywords.Count;
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
