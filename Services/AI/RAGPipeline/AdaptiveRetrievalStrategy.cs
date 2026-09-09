using AIChatAssistant.Common;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.RAG;
using Microsoft.Extensions.Options;

namespace AIChatAssistant.Services.AI.RAGPipeline
{
    public class AdaptiveRetrievalStrategy
    {
        private readonly AdaptiveRetrievalOptions _options;
        private readonly IQueryComplexityAnalyzer _complexityAnalyzer;

        public AdaptiveRetrievalStrategy(
            IOptions<AdaptiveRetrievalOptions> options,
            IQueryComplexityAnalyzer complexityAnalyzer)
        {
            _options = options.Value;
            _complexityAnalyzer = complexityAnalyzer;
        }

        public async Task<(int TopK, float Threshold)>
            GetStrategyAsync(string question)
        {
            var analysis =
                await _complexityAnalyzer.AnalyzeAsync(
                    question);

            Console.WriteLine(
                "========= ADAPTIVE RETRIEVAL =========");

            Console.WriteLine(
                $"Complexity: {analysis.Complexity}");

            Console.WriteLine(
                $"Score: {analysis.Score:F3}");

            Console.WriteLine(
                $"Reason: {analysis.Reason}");

            var strategy =
                analysis.Complexity switch
                {
                    QueryComplexity.Simple =>
                        (
                            _options.SimpleTopK,
                            _options.SimpleThreshold
                        ),

                    QueryComplexity.Complex =>
                        (
                            _options.ComplexTopK,
                            _options.ComplexThreshold
                        ),

                    _ =>
                        (
                            _options.NormalTopK,
                            _options.NormalThreshold
                        )
                };

            Console.WriteLine(
                $"Selected TopK: {strategy.Item1}");

            Console.WriteLine(
                $"Selected Threshold: " +
                $"{strategy.Item2:F3}");

            Console.WriteLine(
                "=======================================");

            return strategy;
        }
    }
}
