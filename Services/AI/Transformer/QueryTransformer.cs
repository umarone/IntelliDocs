using AIChatAssistant.Interfaces;

namespace AIChatAssistant.Services.AI.Transformer
{
    public class QueryTransformer : IQueryTransformer
    {
        private readonly IChatCompletionService _chatCompletionService;

        public QueryTransformer(
            IChatCompletionService chatCompletionService)
        {
            _chatCompletionService = chatCompletionService;
        }
        public async Task<string> TransformAsync(
        string query,
        CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return query;
            }

            const string systemPrompt =
                """
            You are a search query optimizer.

            Your task is to transform the user's question into a concise
            search query suitable for a knowledge retrieval system.

            Rules:
            1. Preserve the original meaning.
            2. Keep important technical terms.
            3. Remove unnecessary conversational words.
            4. Do not answer the question.
            5. Return only the optimized search query.
            """;

            var result =
                await _chatCompletionService.CompleteAsync(
                    systemPrompt,
                    query,
                    cancellationToken);

            return result.Trim();
        }
    }
}
