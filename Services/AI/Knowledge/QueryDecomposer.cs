using AIChatAssistant.Common;
using AIChatAssistant.Interfaces;

namespace AIChatAssistant.Services.AI.Knowledge
{
    public class QueryDecomposer : IQueryDecomposer
    {
        private readonly IChatCompletionService _chatCompletionService;
        private readonly IQueryComplexityAnalyzer _complexityAnalyzer;
        public QueryDecomposer(
            IChatCompletionService chatCompletionService, IQueryComplexityAnalyzer complexityAnalyzer)
        {
            _chatCompletionService = chatCompletionService;
            _complexityAnalyzer = complexityAnalyzer;
        }

        public async Task<IReadOnlyList<string>> DecomposeAsync(
            string question,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return [];
            }

            const string systemPrompt =
            """
            You decompose user requests for a knowledge retrieval system.

            Split the user's request only when it explicitly contains multiple
            independent information needs.

            If there is only one information need, return the user's input unchanged.

            If there are multiple information needs, return one concise query for
            each explicitly requested information need.

            Do not answer the user.
            Do not explain.
            Do not add information.
            Do not invent information needs.
            Do not generate related questions.
            Do not rewrite a single-information request.
            Do not output numbers, bullets, labels, or commentary.

            Output only the queries, one per line.
            Maximum three queries.
            """;

            var result =
                await _chatCompletionService.CompleteAsync(
                    systemPrompt,
                    question,
                    cancellationToken);

            var queries =
                result
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

            return queries.Count > 0
                ? queries
                : [question];
        }
        public async Task<IReadOnlyList<string>> DecomposeIfNeededAsync(
    string question,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return [];
            }

            var analysis =
                await _complexityAnalyzer.AnalyzeAsync(question);

            Console.WriteLine(
                "========= QUERY DECOMPOSITION DECISION =========");

            Console.WriteLine(
                $"Question: {question}");

            Console.WriteLine(
                $"Complexity: {analysis.Complexity}");

            Console.WriteLine(
                $"Score: {analysis.Score:F3}");

            Console.WriteLine(
                $"Reason: {analysis.Reason}");

            if (analysis.Complexity == QueryComplexity.Simple)
            {
                Console.WriteLine(
                    "Decomposition skipped: simple query.");

                Console.WriteLine(
                    "=================================================");

                return [question];
            }

            Console.WriteLine(
                "Decomposition required.");

            Console.WriteLine(
                "=================================================");

            return await DecomposeAsync(
                question,
                cancellationToken);
        }
    }
}
