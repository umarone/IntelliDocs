using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Services.AI.Knowledge
{
    public class RetrievalConfidenceCalculator
    {
        public float Calculate(
    IReadOnlyList<SearchResult> results,
    RetrievalValidationResult validation)
        {
            if (results.Count == 0)
            {
                return 0f;
            }

            var bestRerankScore =
                results.Max(x => x.RerankScore);

            var averageRerankScore =
                results.Average(x => x.RerankScore);

            var validatorConfidence =
                validation.Confidence;

            // Weighted confidence:
            // 50% retrieval/reranking quality
            // 50% LLM retrieval validation
            var retrievalScore =
                (bestRerankScore * 0.6f) +
                (averageRerankScore * 0.4f);

            var confidence =
                (retrievalScore * 0.5f) +
                (validatorConfidence * 0.5f);

            return Math.Clamp(
                confidence,
                0f,
                1f);
        }
    }
}
