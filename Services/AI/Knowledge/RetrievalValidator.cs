using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.Knowledge
{
    public class RetrievalValidator : IRetrievalValidator
    {
        private readonly IChatCompletionService _chatCompletionService;

        public RetrievalValidator(
            IChatCompletionService chatCompletionService)
        {
            _chatCompletionService = chatCompletionService;
        }

        public async Task<RetrievalValidationResult> ValidateAsync(
    string question,
    IReadOnlyList<SearchResult> results,
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new RetrievalValidationResult
                {
                    IsRelevant = false,
                    Confidence = 0,
                    Reason = "The question is empty."
                };
            }

            if (results.Count == 0)
            {
                return new RetrievalValidationResult
                {
                    IsRelevant = false,
                    Confidence = 0,
                    Reason = "No retrieved context was available."
                };
            }

            var systemPrompt =
        """
        You are a retrieval quality evaluator.

        Determine whether the RETRIEVED CONTEXT contains information that
        can directly help answer the USER QUESTION.

        Rules:
        1. Use only the retrieved context for the relevance decision.
        2. Do not answer the user question.
        3. Do not use outside or pretrained knowledge.
        4. Partial but clearly relevant information is sufficient.
        5. Shared words alone do not establish relevance.
        6. Do not infer information that is not present in the context.
        7. Set isRelevant to true when the context can directly help answer
           the question.
        8. Set isRelevant to false when the context is unrelated or cannot
           help answer the question.
        9. Confidence must be between 0 and 1.
        10. Provide a short reason for the decision.

        Return only valid JSON:

        {
          "isRelevant": true or false,
          "confidence": 0.0,
          "reason": "short explanation"
        }

        Do not return markdown or additional text.
        """;

            var userPrompt =
                $"""
        USER QUESTION
        ========================================
        {question}

        ========================================
        RETRIEVED CONTEXT
        ========================================

        {string.Join(
                    "\n",
                    results
                        .Select(r => r.Record.Content)
                        .Where(c => !string.IsNullOrWhiteSpace(c)))}

        ========================================
        """;

            var result =
                await _chatCompletionService.CompleteAsync(
                    systemPrompt,
                    userPrompt,
                    cancellationToken);

            var content = CleanJsonResponse(result); //result.Trim();

            Console.WriteLine(
                "========= RETRIEVAL VALIDATOR RESPONSE =========");

            Console.WriteLine(content);

            Console.WriteLine(
                "=================================================");

            try
            {
                var validation =
                    JsonSerializer.Deserialize<RetrievalValidationResult>(
                        content,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (validation is not null)
                {
                    validation.Confidence =
                        Math.Clamp(
                            validation.Confidence,
                            0f,
                            1f);

                    return validation;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Retrieval validator JSON error: {ex.Message}");
            }

            return new RetrievalValidationResult
            {
                IsRelevant = false,
                Confidence = 0,
                Reason =
                    "The retrieval validator returned an invalid response."
            };
        }
        private static string CleanJsonResponse(string content)
        {
            content = content.Trim();

            if (content.StartsWith(
                    "```json",
                    StringComparison.OrdinalIgnoreCase))
            {
                content =
                    content["```json".Length..].Trim();
            }
            else if (content.StartsWith(
                         "```",
                         StringComparison.OrdinalIgnoreCase))
            {
                content =
                    content["```".Length..].Trim();
            }

            if (content.EndsWith(
                    "```",
                    StringComparison.OrdinalIgnoreCase))
            {
                content =
                    content[..^"```".Length].Trim();
            }

            return content;
        }
    }
}
