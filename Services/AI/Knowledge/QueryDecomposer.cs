using AIChatAssistant.Common;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.RAG;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.Knowledge
{
    public class QueryDecomposer : IQueryDecomposer
    {
        private readonly IChatCompletionService _chatCompletionService;
        private readonly IQueryComplexityAnalyzer _complexityAnalyzer;
        private readonly IInformationNeedDetector _informationNeedDetector;
        public QueryDecomposer(
            IChatCompletionService chatCompletionService, 
            IQueryComplexityAnalyzer complexityAnalyzer,
            IInformationNeedDetector informationNeedDetector)
        {
            _chatCompletionService = chatCompletionService;
            _complexityAnalyzer = complexityAnalyzer;
            _informationNeedDetector = informationNeedDetector;
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

            Your task is ONLY to identify explicitly requested independent
            information needs.

            RULES

            1. If the request contains only one information need,
               return the original user request unchanged as the only query.

            2. If the request explicitly contains multiple independent
               information needs, return one query for each information need.

            3. Preserve the original meaning and requested scope.

            4. Do not introduce new information.

            5. Do not invent information needs.

            6. Do not generate related questions.

            7. Do not broaden the user's request.

            8. Do not answer the user.

            9. Do not explain anything.

            10. Do not rewrite a single-information request.

            Return ONLY valid JSON using this exact structure:

            {
              "queries": [
                "query 1",
                "query 2"
              ]
            }

            Maximum three queries.
            """;

            var response =
                await _chatCompletionService.CompleteStructuredAsync(
                    systemPrompt,
                    question,
                    cancellationToken);

            Console.WriteLine(
                "========= QUERY DECOMPOSITION RESPONSE =========");

            Console.WriteLine(response);

            Console.WriteLine(
                "=================================================");

            QueryDecompositionResult? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<QueryDecompositionResult>(
                        response,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Query decomposition JSON error: {ex.Message}");

                return [question];
            }

            if (result is null ||
                result.Queries is null ||
                result.Queries.Count == 0)
            {
                Console.WriteLine(
                    "Query decomposition returned no queries.");

                return [question];
            }

            var queries =
                result.Queries
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .Select(q => q.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

            if (queries.Count == 0)
            {
                return [question];
            }

            return queries;
        }
        public async Task<
    (IReadOnlyList<string> InformationNeeds,
     QueryComplexityResult ComplexityAnalysis)>
    DecomposeIfNeededAsync(
        string question,
        CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return (
                    [],
                    new QueryComplexityResult
                    {
                        Complexity = QueryComplexity.Normal,
                        Score = 0.5,
                        Reason = "Question was empty or invalid."
                    });
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

            // Simple questions are still passed through the
            // information-need detector because complexity and
            // number of information needs are separate concerns.

            var informationNeeds =
                await _informationNeedDetector.DetectAsync(
                    question,
                    cancellationToken);

            Console.WriteLine(
                $"Detected count: {informationNeeds.InformationNeeds.Count}");

            Console.WriteLine(
                $"HasMultipleNeeds: {informationNeeds.HasMultipleNeeds}");

            Console.WriteLine(
                $"Reason: {informationNeeds.Reason}");

            Console.WriteLine(
                "Information needs detected:");

            foreach (var need in informationNeeds.InformationNeeds)
            {
                Console.WriteLine(
                    $"- {need}");
            }

            if (!informationNeeds.HasMultipleNeeds)
            {
                Console.WriteLine(
                    "Decomposition skipped: only one information need.");

                Console.WriteLine(
                    "=================================================");

                return (new List<string> { question }, analysis);
            }

            Console.WriteLine(
                $"Multiple information needs detected: " +
                $"{informationNeeds.InformationNeeds.Count}");

            Console.WriteLine(
                "=================================================");

            return (
                informationNeeds.InformationNeeds,
                analysis);
        }
    }
}
