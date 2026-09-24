using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.RAG;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.RAGPipeline
{
    public class InformationNeedDetector : IInformationNeedDetector
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly OllamaOptions _options;

        public InformationNeedDetector(
            IOllamaClient ollamaClient,
            IOptions<OllamaOptions> options)
        {
            _ollamaClient = ollamaClient;
            _options = options.Value;
        }

        public async Task<InformationNeedDetectionResult> DetectAsync(
            string question,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new InformationNeedDetectionResult
                {
                    HasMultipleNeeds = false,
                    InformationNeeds = [],
                    Reason = "Question was empty or invalid."
                };
            }

            var prompt = BuildPrompt(question);

            var request = new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,

                Messages =
                [
                    new OllamaChatMessage
                {
                    Role = "system",
                    Content = prompt
                }
                ],

                Format = new
                {
                    type = "object",
                    properties = new
                    {
                        informationNeeds = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "string"
                            }
                        }
                    },
                    required = new[]
                    {
                    "informationNeeds"
                },
                    additionalProperties = false
                },

                Options = new OllamaGenerationOptions
                {
                    Temperature = 0,
                    Seed = 42
                }
            };

            var response =
                await _ollamaClient.SendAsync(request);

            Console.WriteLine(
    "========= INFORMATION NEED DETECTOR RAW RESPONSE =========");

            Console.WriteLine(
                response.Message.Content);

            Console.WriteLine(
                "==========================================================");

            var result =
                JsonSerializer.Deserialize<InformationNeedDetectionResponse>(
                    response.Message.Content);

            return ValidateResult(
                question,
                result);
        }


    private static string BuildPrompt(string question)
    {
            const string prompt = """
    You are an information-need detector for a generic knowledge retrieval system.

    Your task is to identify ALL independent information needs explicitly requested
    by the user.

    The question may refer to ANY subject, domain, document, person, technology,
    process, product, organization, policy, or other topic.

    IMPORTANT:
    You must identify the complete set of independent information needs.
    If the user explicitly asks for multiple independent pieces of information,
    return ALL of them. Do not stop after identifying the first one.

    STRICT RULES
    ========================================

    1. Preserve the user's original meaning.

    2. Identify separate information needs only when the user is explicitly asking
       for independent pieces of information.

    3. Do not split a single conceptual question into smaller questions merely
       because it contains multiple concepts, terms, entities, or subjects.

    4. A concept, entity, term, subject, object, or other element mentioned within
       a question is NOT automatically a separate information need.

    5. A referenced concept or entity becomes a separate information need only when
       the user explicitly requests information about that concept or entity as
       an independent part of the question.

    6. Do not invent information needs.

    7. Do not broaden the user's question.

    8. Do not narrow the user's question.

    9. Do not answer the question.

    10. Do not use outside knowledge.

    11. Preserve important terms, entities, qualifiers, relationships, and scope
        from the original question.

    12. If the question contains only one information need, return the original
        question unchanged as the only information need.

    13. Return the smallest valid set of independent information needs.

    14. Every returned item must represent something the user is actually asking
        to know, determine, compare, explain, identify, or otherwise obtain.

    15. Do not return standalone concepts, entities, keywords, labels, or subjects
        unless the user explicitly asks for information about them as an
        independent need.

    16. Do not return answers, facts, explanations, conclusions, or inferred
        information.

    17. When multiple independent requests are present, identify EVERY independent
        request before producing the output.

    18. Do not stop processing the question after finding the first information need.

    19. The word "and" does NOT automatically mean that a question contains multiple
        information needs. Split only when the user explicitly requests independent
        information.

    20. Return ONLY valid JSON.

    EXAMPLES
    ========================================

    Example 1:

    USER QUESTION:
    What is X and what is Y?

    INFORMATION NEEDS:
    [
      "What is X?",
      "What is Y?"
    ]

    Example 2:

    USER QUESTION:
    What is X and how does Y work?

    INFORMATION NEEDS:
    [
      "What is X?",
      "How does Y work?"
    ]

    Example 3:

    USER QUESTION:
    What is X and Y?

    INFORMATION NEEDS:
    [
      "What is X and Y?"
    ]

    Example 4:

    USER QUESTION:
    How does X work and what are its limitations?

    INFORMATION NEEDS:
    [
      "How does X work?",
      "What are its limitations?"
    ]

    IMPORTANT EXAMPLE RULE:
    In examples containing multiple independent requests, ALL independent requests
    must be returned.

    USER QUESTION
    ========================================

    """;

            const string outputFormat = """

    OUTPUT FORMAT
    ========================================

    Return the actual information needs extracted from the USER QUESTION.

    Do NOT return placeholder text.

    First determine whether the USER QUESTION contains one or multiple independent
    information needs.

    If the USER QUESTION contains multiple independent information needs,
    return one concise question for EACH independent need.

    If the USER QUESTION contains only one information need,
    return the original question unchanged as the only item.

    The returned information needs must:

    - Preserve the original meaning.
    - Preserve important terms and scope.
    - Represent actual user requests.
    - Include ALL explicitly requested independent information.
    - Not introduce new concepts.
    - Not remove required information.
    - Not answer the question.
    - Not contain explanations.
    - Not contain standalone concepts or entities unless independently requested.
    - Not stop after returning the first information need.

    Return ONLY valid JSON using this structure:

    {
      "informationNeeds": [
        "actual information need"
      ]
    }
    """;

            return prompt + question + outputFormat;
        }


        private static InformationNeedDetectionResult ValidateResult(
        string question,
        InformationNeedDetectionResponse? result)
            {
                var needs =
                    result?.InformationNeeds?
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.Ordinal)
                        .ToList()
                    ?? [];

                // No valid detector output.
                if (needs.Count == 0)
                {
                    return new InformationNeedDetectionResult
                    {
                        HasMultipleNeeds = false,
                        InformationNeeds = [question],
                        Reason = "Detector returned no valid information needs."
                    };
                }

                // A single information need must preserve the
                // original user question unchanged.
                if (needs.Count == 1 &&
                    !string.Equals(
                        needs[0],
                        question.Trim(),
                        StringComparison.Ordinal))
                {
                    return new InformationNeedDetectionResult
                    {
                        HasMultipleNeeds = false,
                        InformationNeeds = [question.Trim()],
                        Reason =
                            "Detector returned a single rewritten need. " +
                            "The rewritten need was rejected to preserve the original question."
                    };
                }

                return new InformationNeedDetectionResult
                {
                    HasMultipleNeeds = needs.Count > 1,
                    InformationNeeds = needs,
                    Reason =
                        $"Detected {needs.Count} information need(s)."
                };
            }
    }
}
