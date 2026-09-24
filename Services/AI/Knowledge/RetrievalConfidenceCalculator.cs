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

            var bestSimilarity =
                results.Max(x => x.Similarity);

            var averageSimilarity =
                results.Average(x => x.Similarity);

            var validatorConfidence =
                validation.Confidence;

            // Weighted retrieval quality:
            // 60% best semantic similarity
            // 40% average semantic similarity
            var retrievalScore =
                (bestSimilarity * 0.6f) +
                (averageSimilarity * 0.4f);

            // Final confidence:
            // 50% retrieval quality
            // 50% LLM validation
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
