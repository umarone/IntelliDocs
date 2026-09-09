using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.Validators;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.Validators
{
    public class GroundingValidator : IGroundingValidator
    {
        private readonly IOllamaClient _ollamaClient;

        public GroundingValidator(
            IOllamaClient ollamaClient)
        {
            _ollamaClient = ollamaClient;
        }
        public async Task<GroundingValidationResult> ValidateAsync(
            string question,
            string answer,
            IReadOnlyList<SearchResult> searchResults)
        {

            var directCheckStart = Stopwatch.GetTimestamp();

            var directlySupported =
                IsDirectlySupported(
                    answer,
                    searchResults);

            var directCheckElapsed =
                Stopwatch.GetElapsedTime(directCheckStart);

            Console.WriteLine(
                $"Grounding deterministic check: " +
                $"{directCheckElapsed.TotalMilliseconds:F0} ms");

            if (directlySupported)
            {
                Console.WriteLine(
                    "Grounding validation skipped: " +
                    "answer is directly supported by the knowledge base.");

                return new GroundingValidationResult
                {
                    Grounded = true,
                    Reason =
                        "The generated answer is directly supported by the knowledge base."
                };
            }

            Console.WriteLine("========= SEARCH RESULTS BEFORE KNOWLEDGE BASE =========");

            foreach (var result in searchResults)
            {
                Console.WriteLine("----- RESULT -----");
                Console.WriteLine(result.Record.Content);
            }


            var knowledgeBase =
                searchResults.Count == 0
                    ? "NO KNOWLEDGE AVAILABLE"
                    : string.Join(
                        "\n",
                        searchResults
                            .Select(r => r.Record.Content)
                            .Where(c => !string.IsNullOrWhiteSpace(c)));

            string systemPrompt = """
You are a strict grounding validator for a knowledge retrieval system.

Your task is to determine whether the GENERATED ANSWER is fully supported
by the KNOWLEDGE BASE.

GROUNDING RULES
========================================

1. Validate only factual claims that are actually stated in the GENERATED ANSWER.

2. First identify the factual claims made by the GENERATED ANSWER.
   Do not create, infer, reconstruct, or add claims that the answer does not state.

3. The KNOWLEDGE BASE is evidence only.
   Facts that appear in the KNOWLEDGE BASE but are not stated in the
   GENERATED ANSWER are irrelevant to the grounding decision.

4. Do not use pretrained knowledge, outside information, assumptions,
   or general knowledge.

5. A claim is grounded when its factual meaning is explicitly supported
   by the KNOWLEDGE BASE, including faithful paraphrasing or summarization.

6. A claim is not grounded when it adds information, changes the meaning,
   or requires information that is not supported by the KNOWLEDGE BASE.

7. The USER QUESTION is not evidence. Use it only to understand what
   the GENERATED ANSWER is addressing.

8. If the GENERATED ANSWER contains multiple factual claims, every claim
   must be supported for the answer to be considered grounded.

9. If the GENERATED ANSWER contains no factual claims, consider it grounded.

10. Do not reject an answer because the KNOWLEDGE BASE contains additional
    information that the answer does not mention.

11. Return grounded=true only when the GENERATED ANSWER is fully supported
    by the KNOWLEDGE BASE.

12. Return grounded=false if any factual claim in the GENERATED ANSWER
    is unsupported.

OUTPUT FORMAT
========================================

Return only valid JSON:

{
  "grounded": true or false,
  "reason": "brief reason"
}

For grounded=true, the reason must state that the factual claims in the
GENERATED ANSWER are supported by the KNOWLEDGE BASE.

For grounded=false, identify the unsupported claim without adding facts
from your own knowledge.

Do not return markdown, explanations outside the JSON, or additional fields.
""";


           

            string userPrompt = $@"========================================
            KNOWLEDGE BASE
            ========================================

            {knowledgeBase}

            ========================================
            USER QUESTION
            ========================================

            {question}

            ========================================
            GENERATED ANSWER
            ========================================

            {answer}";

            var schema = new
            {
                type = "object",
                properties = new
                {
                    grounded = new
                    {
                        type = "boolean"
                    },
                    reason = new
                    {
                        type = "string"
                    }
                },
                required = new[] { "grounded", "reason" },
                additionalProperties = false
            };

            Console.WriteLine("========= KB SENT TO GROUNDING =========");
            Console.WriteLine(knowledgeBase);

            Console.WriteLine("========= ANSWER SENT TO GROUNDING =========");
            Console.WriteLine(answer);


            var request = new OllamaChatRequest
            {
                Model = "llama3.2",
                Stream = false,
                Format = schema,

                Options = new OllamaGenerationOptions
                {
                    Temperature = 0f,
                    Seed = 42
                },

                Messages =
                [
                    new OllamaChatMessage
                    {
                        Role = "system",
                        Content = systemPrompt
                    },
                    new OllamaChatMessage
                    {
                        Role = "user",
                        Content = userPrompt
                    }
                ]
            };

            var response =
                await _ollamaClient.SendAsync(request);

            var content =
                response.Message.Content.Trim();

            Console.WriteLine(
                "========= GROUNDING VALIDATOR RESPONSE =========");

            Console.WriteLine(content);

            Console.WriteLine(
                "=================================================");

            try
            {
                var result =
                    JsonSerializer.Deserialize<GroundingValidationResult>(
                        content,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (result is not null)
                {
                    return result;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Grounding validator JSON error: {ex.Message}");
            }

            return new GroundingValidationResult
            {
                Grounded = false,
                Reason =
                    "The grounding validator returned an invalid response."
            };
        }
        private static bool IsDirectlySupported(
     string answer,
     IReadOnlyList<SearchResult> searchResults)
        {
            if (string.IsNullOrWhiteSpace(answer) ||
                searchResults.Count == 0)
            {
                return false;
            }

            var normalizedAnswer =
                NormalizeClaim(answer);

            if (string.IsNullOrWhiteSpace(normalizedAnswer))
            {
                return false;
            }

            foreach (var result in searchResults)
            {
                var content = result.Record.Content;

                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var normalizedContent =
                    NormalizeClaim(content);

                // Exact normalized match.
                if (normalizedContent.Contains(
                    normalizedAnswer,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Compare the answer against individual KB sentences.
                var sentences =
                    content
                        .Split(
                            ['.', '!', '?'],
                            StringSplitOptions.RemoveEmptyEntries);

                foreach (var sentence in sentences)
                {
                    var normalizedSentence =
                        NormalizeClaim(sentence);

                    if (string.IsNullOrWhiteSpace(normalizedSentence))
                    {
                        continue;
                    }

                    if (ClaimsHaveSameMeaningStructure(
                        normalizedAnswer,
                        normalizedSentence))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ClaimsHaveSameMeaningStructure(
            string answer,
            string source)
        {
            var answerTokens =
                GetClaimTokens(answer);

            var sourceTokens =
                GetClaimTokens(source);

            if (answerTokens.Count == 0 ||
                sourceTokens.Count == 0)
            {
                return false;
            }

            var commonTokens =
                answerTokens
                    .Intersect(
                        sourceTokens,
                        StringComparer.OrdinalIgnoreCase)
                    .Count();

            var smallerCount =
                Math.Min(
                    answerTokens.Count,
                    sourceTokens.Count);

            if (smallerCount == 0)
            {
                return false;
            }

            var overlap =
                (float)commonTokens / smallerCount;

            // Require strong overlap and reasonably similar claim size.
            var sizeDifference =
                Math.Abs(
                    answerTokens.Count -
                    sourceTokens.Count);

            return overlap >= 0.90f &&
                   sizeDifference <= 2;
        }

        private static string NormalizeClaim(string text)
        {
            return string.Join(
                " ",
                text
                    .ToLowerInvariant()
                    .Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(token =>
                        token.Trim(
                            '.', ',', '!', '?', ':', ';',
                            '"', '\'', '(', ')', '[', ']'))
                    .Where(token =>
                        !string.IsNullOrWhiteSpace(token)));
        }

        private static List<string> GetClaimTokens(string text)
        {
            var ignoredWords =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
            "is",
            "are",
            "was",
            "were",
            "be",
            "being",
            "been",
            "stands",
            "for",
            "the",
            "a",
            "an"
                };

            return text
                .Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(token =>
                    token.Trim(
                        '.', ',', '!', '?', ':', ';',
                        '"', '\'', '(', ')', '[', ']'))
                .Where(token =>
                    token.Length >= 2 &&
                    !ignoredWords.Contains(token))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> GetMeaningfulTokens(string text)
        {
            return text
                .Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(token =>
                    token.Trim(
                        '.', ',', '!', '?', ':', ';',
                        '"', '\'', '(', ')', '[', ']'))
                .Where(token =>
                    token.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeText(string text)
        {
            return string.Join(
                " ",
                text
                    .Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
