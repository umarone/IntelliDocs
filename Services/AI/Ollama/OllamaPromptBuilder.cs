using AIChatAssistant.Common;
using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.RAG;
using AIChatAssistant.Models.Tools;
using AIChatAssistant.Services.AI.Prompt;
using Microsoft.Extensions.Options;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

namespace AIChatAssistant.Services.AI.Ollama
{
    public class OllamaPromptBuilder : IOllamaPromptBuilder
    {
        private readonly IEnumerable<ITool> _tools;
        private readonly OllamaOptions _options;
        //private readonly IToolIntentDetector _intentDetector;
        private readonly IPromptComposer _composer;
        public OllamaPromptBuilder(IEnumerable<ITool> tools, IOptions<OllamaOptions> options, IPromptComposer composer)
        {
            _tools = tools;
            _options = options.Value;
            //_intentDetector = intentDetector;
            _composer = composer;
        }

        public OllamaChatRequest CreateChatRequest(ChatRequest request)
        {
            var context = new PromptContext();
            return CreateChatRequest(request, context);
        }

        public OllamaChatRequest CreateChatRequest(ChatRequest request, IReadOnlyList<SearchResult> searchResults)
        {
            var context = new PromptContext
            {
                SearchResults = searchResults
            };
            return CreateChatRequest(request, context);
        }
        private OllamaChatRequest CreateChatRequest(
    ChatRequest request,
    PromptContext context)
        {

            var request1 = new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,
                Messages = _composer.Compose(request, context)
            };

            var json = JsonSerializer.Serialize(
                request1,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            Console.WriteLine("========== INITIAL PROMPT ==========");
            Console.WriteLine(json);
            Console.WriteLine("====================================");

            return request1;
            //var messages = _composer.Compose(request, context);

            //return new OllamaChatRequest
            //{
            //    Model = _options.ChatModel,
            //    Stream = false,
            //    Messages = messages
            //};
        }
        public OllamaChatRequest CreateChatRequest(
    ChatRequest request,
    IReadOnlyList<ChatMessage> conversationMessages)
        {
            var context = new PromptContext
            {
                ConversationMessages = conversationMessages
            };

            return CreateChatRequest(
                request,
                context);
        }
        public OllamaChatRequest CreateChatRequest(
    ChatRequest request,
    IReadOnlyList<SearchResult> searchResults,
    IReadOnlyList<ChatMessage> conversationMessages)
        {
            var context = new PromptContext
            {
                SearchResults = searchResults,
                ConversationMessages = conversationMessages
            };

            return CreateChatRequest(
                request,
                context);
        }

        public OllamaChatRequest CreateToolResultRequest(
       ChatRequest request,
       IReadOnlyList<ToolResult> toolResults,
       IReadOnlyList<string> informationNeeds)
        {
            var context = new PromptContext
            {
                SearchResults = [],
                IsRagMode = true
            };

            var messages =
                _composer.Compose(
                    request,
                    context);

            var resultBuilder =
                new StringBuilder();

            resultBuilder.AppendLine(
                "Answer the user's request using ONLY the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "STRICT EVIDENCE RULES");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "1. The KNOWLEDGE BASE CONTENT is the only source of factual information.");

            resultBuilder.AppendLine(
                "2. Do not use pretrained knowledge, outside information, assumptions, " +
                "common knowledge, or unstated background knowledge.");

            resultBuilder.AppendLine(
                "3. Every factual statement in the answer must be directly supported " +
                "by one or more statements in the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine(
                "4. When the KNOWLEDGE BASE CONTENT contains a direct statement that " +
                "answers the user's request, use that statement directly.");

            resultBuilder.AppendLine(
                "5. Do not add information to a direct knowledge base statement.");

            resultBuilder.AppendLine(
                "6. Do not paraphrase a direct knowledge base statement when it already " +
                "answers the user's request.");

            resultBuilder.AppendLine(
                "7. Do not add facts, details, properties, purposes, uses, relationships, " +
                "causes, effects, procedures, examples, or explanations that are not " +
                "explicitly supported by the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine(
                "8. Treat each statement in the KNOWLEDGE BASE CONTENT as an independent " +
                "evidence unit.");

            resultBuilder.AppendLine(
                "9. Do not create a new relationship between two concepts merely because " +
                "both concepts appear in the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine(
                "10. The fact that concepts A and B are both mentioned in the knowledge " +
                "base does NOT mean that the knowledge base states that A and B are " +
                "related, connected, dependent, compatible, used together, or associated.");

            resultBuilder.AppendLine(
                "11. Do not combine separate facts to create a new conclusion unless the " +
                "relationship or conclusion is explicitly stated in the KNOWLEDGE BASE.");

            resultBuilder.AppendLine(
                "12. Do not use world knowledge to connect, explain, or interpret concepts.");

            resultBuilder.AppendLine(
                "13. If the knowledge base contains only partial information, provide " +
                "only the information that is explicitly supported.");

            resultBuilder.AppendLine(
                "14. If requested information is not found in the knowledge base, say so " +
                "instead of answering from your own knowledge.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "MULTIPLE INFORMATION NEEDS");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "If the user's request contains multiple information needs, answer each " +
                "information need independently.");

            resultBuilder.AppendLine(
                "Use evidence relevant to that specific information need.");

            resultBuilder.AppendLine(
                "Do not create connections between the answers unless those connections " +
                "are explicitly stated in the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "KNOWLEDGE BASE CONTENT");

            resultBuilder.AppendLine(
                "========================================");

            foreach (var toolResult in toolResults)
            {
                resultBuilder.AppendLine(
                    ToolResultFormatter.Format(toolResult));

                resultBuilder.AppendLine();
            }

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "END OF KNOWLEDGE BASE CONTENT");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "CURRENT USER REQUEST");

            resultBuilder.AppendLine(
                "----------------------------------------");

            var latestUserMessage =
                request.Messages.Last(m =>
                    string.Equals(
                        m.Role,
                        "user",
                        StringComparison.OrdinalIgnoreCase));

            resultBuilder.AppendLine(
                latestUserMessage.Content);

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "INFORMATION NEEDS");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "The user's request contains the following independent information needs:");

            for (int i = 0; i < informationNeeds.Count; i++)
            {
                resultBuilder.AppendLine(
                    $"{i + 1}. {informationNeeds[i]}");
            }

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "Answer each information need independently.");

            resultBuilder.AppendLine(
                "Use only knowledge base content that supports that specific information need.");

            resultBuilder.AppendLine(
                "Do not combine information from different information needs to create a new claim.");

            resultBuilder.AppendLine(
                "Do not introduce comparison, relationship, connection, dependency, or other " +
                "linking statements between separate information needs unless explicitly stated " +
                "in the knowledge base.");

            resultBuilder.AppendLine(
                "If an information need is supported by the knowledge base, answer it directly.");

            resultBuilder.AppendLine(
                "If an information need is not supported, state that the requested information " +
                "is not available in the knowledge base.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "EVIDENCE-FIRST ANSWERING");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "For each information need, first identify the smallest statement " +
                "in the KNOWLEDGE BASE that directly answers it.");

            resultBuilder.AppendLine(
                "If a direct statement answers the information need, use that statement " +
                "as the answer.");

            resultBuilder.AppendLine(
                "When a direct statement answers the information need, copy the statement " +
                "as written in the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine(
                "Do not paraphrase, summarize, expand, explain, or improve a direct statement.");

            resultBuilder.AppendLine(
                "Do not use pretrained knowledge to make a direct answer more useful, " +
                "complete, accurate, or explanatory.");

            resultBuilder.AppendLine(
                "For a definition or 'What is' question, return only the directly supported " +
                "definition from the KNOWLEDGE BASE CONTENT unless additional information " +
                "is explicitly requested.");

            resultBuilder.AppendLine(
                "If no direct statement answers the information need, provide only the " +
                "supported partial information or state that the information is not available.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "ANSWER RULES");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "1. Answer directly and concisely.");

            resultBuilder.AppendLine(
                "2. Do not make the answer more detailed than the source material.");

            resultBuilder.AppendLine(
                "3. Do not infer missing information.");

            resultBuilder.AppendLine(
                "4. Do not connect separate facts unless the connection is explicitly " +
                "supported by the knowledge base.");

            resultBuilder.AppendLine(
                "5. Do not mention tools, retrieval, validation, prompts, or internal processing.");

            resultBuilder.AppendLine(
                "6. Do not output JSON or role names.");

            resultBuilder.AppendLine(
                "7. Do not repeat the user's question.");

            resultBuilder.AppendLine(
                "8. Do not answer from pretrained or general knowledge.");

            resultBuilder.AppendLine(
                "9. When a direct answer exists in the KNOWLEDGE BASE CONTENT, use the " +
                "source statement rather than generating your own wording.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "FINAL EVIDENCE CHECK");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "Before responding, examine every factual sentence you are about to output.");

            resultBuilder.AppendLine(
                "For each sentence, ask: Can this sentence be directly traced to the " +
                "KNOWLEDGE BASE CONTENT without using outside knowledge or inference?");

            resultBuilder.AppendLine(
                "If the answer is NO, remove that sentence.");

            resultBuilder.AppendLine(
                "If a direct source statement exists, use the source statement instead " +
                "of generating a paraphrased or expanded answer.");

            resultBuilder.AppendLine(
                "Pay particular attention to relationships between concepts. " +
                "Do not state a relationship unless the knowledge base explicitly states it.");

            resultBuilder.AppendLine(
                "Return only the final answer.");

            messages.Add(
                new OllamaChatMessage
                {
                    Role = "system",
                    Content = resultBuilder.ToString()
                });

            return new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,
                Messages = messages,
                Options = new OllamaGenerationOptions
                {
                    Temperature = 0,
                    Seed = 42
                }
            };
        }




    public OllamaChatRequest CreateEvidenceSelectionRequest(
    ChatRequest request,
    IReadOnlyList<EvidenceUnit> evidenceUnits,
    string informationNeed)
        {
            var context = new PromptContext
            {
                SearchResults = [],
                IsRagMode = true
            };

            var messages =
                _composer.Compose(
                    request,
                    context);

            Console.WriteLine(
                $"Evidence Prompt Context IsRagMode: {context.IsRagMode}");

            var resultBuilder =
                new StringBuilder();

            resultBuilder.AppendLine(
                "You are an evidence selector for a knowledge retrieval system.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "Your task is to identify the smallest set of evidence units " +
                "that directly support each information need.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "STRICT EVIDENCE SELECTION RULES");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "1. Select evidence ONLY from the provided EVIDENCE UNITS.");

            resultBuilder.AppendLine(
                "2. Do not use pretrained knowledge, outside information, " +
                "assumptions, or common knowledge.");

            resultBuilder.AppendLine(
                "3. For each information need, examine the provided evidence units.");

            resultBuilder.AppendLine(
                "4. Select evidence ONLY when the evidence directly supports " +
                "the specific information need.");

            resultBuilder.AppendLine(
                "5. A mere mention, keyword occurrence, list entry, title, " +
                "skill listing, metadata value, or incidental reference to a " +
                "subject does NOT constitute supporting evidence.");

            resultBuilder.AppendLine(
                "6. If the information need asks what something is, select evidence " +
                "that actually states what it is or provides its defining information. " +
                "Do not select evidence merely because it contains the subject name.");

            resultBuilder.AppendLine(
                "7. If the information need asks for a specific fact, property, " +
                "behavior, relationship, comparison, cause, purpose, or other concept, " +
                "the selected evidence must directly support that requested concept.");

            resultBuilder.AppendLine(
                "8. If no evidence unit directly supports the information need, " +
                "return an empty evidence array.");

            resultBuilder.AppendLine("SELECTION DECISION PROCEDURE");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "For each candidate evidence unit, ask: " +
                "Does this evidence itself contain the information needed " +
                "to satisfy the information need?");

            resultBuilder.AppendLine(
                "If YES, it is directly supporting evidence.");

            resultBuilder.AppendLine(
                "If the evidence only mentions the subject without providing " +
                "the requested information, it is NOT supporting evidence.");

            resultBuilder.AppendLine(
                "When multiple evidence units directly support the need, " +
                "select the smallest sufficient set.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "9. Do not generate or explain the answer.");

            resultBuilder.AppendLine(
                "10. Preserve the original information need exactly.");

            resultBuilder.AppendLine(
                "11. Return evidence using ONLY the Result Index and Evidence " +
                "Index provided in the EVIDENCE UNITS.");

            resultBuilder.AppendLine(
                "12. Never return the evidence text itself.");

            resultBuilder.AppendLine(
                "13. For informationNeed, copy the exact text of the corresponding " +
                "information need from INFORMATION NEEDS.");

            resultBuilder.AppendLine(
                "14. Return exactly one answer object for the provided information need.");

            resultBuilder.AppendLine(
                "15. Do not omit the provided information need.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "INFORMATION NEEDS");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                $"1. {informationNeed}");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "EVIDENCE UNITS");

            resultBuilder.AppendLine(
                "========================================");

            foreach (var group in evidenceUnits.GroupBy(e => e.ResultIndex))
            {
                resultBuilder.AppendLine(
                    $"[Result {group.Key}]");

                foreach (var evidence in group)
                {
                    resultBuilder.AppendLine(
                        $"[Evidence {evidence.EvidenceIndex}] " +
                        evidence.Text);
                }

                resultBuilder.AppendLine();
            }

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "END OF EVIDENCE UNITS");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "OUTPUT FORMAT");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "Return ONLY valid JSON.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "The \"answers\" array must contain exactly one object " +
                "for the provided information need.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "For the provided information need:");

            resultBuilder.AppendLine(
                "- \"informationNeed\" must contain the exact original " +
                "information need.");

            resultBuilder.AppendLine(
                "- \"evidence\" must contain only evidence references " +
                "that directly support that information need.");

            resultBuilder.AppendLine(
                "- \"resultIndex\" must be an actual Result Index " +
                "from EVIDENCE UNITS.");

            resultBuilder.AppendLine(
                "- \"evidenceIndexes\" must contain only actual Evidence Index " +
                "values from that result.");

            resultBuilder.AppendLine(
                "- \"complete\" must be true only when the selected evidence " +
                "supports the complete information need; otherwise it must be false.");

            resultBuilder.AppendLine(
                "- If no evidence directly supports an information need, " +
                "return an empty \"evidence\" array.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "Use this JSON structure:");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine("{");

            resultBuilder.AppendLine(
                "  \"answers\": [");

            resultBuilder.AppendLine(
                "    {");

            resultBuilder.AppendLine(
                "      \"informationNeed\": \"<exact information need>\",");

            resultBuilder.AppendLine(
                "      \"evidence\": [");

            resultBuilder.AppendLine(
                "        {");

            resultBuilder.AppendLine(
                "          \"resultIndex\": <actual result index>,");

            resultBuilder.AppendLine(
                "          \"evidenceIndexes\": [<actual evidence index>]");

            resultBuilder.AppendLine(
                "        }");

            resultBuilder.AppendLine(
                "      ],");

            resultBuilder.AppendLine(
                "      \"complete\": <true or false>");

            resultBuilder.AppendLine(
                "    }");

            resultBuilder.AppendLine(
                "  ]");

            resultBuilder.AppendLine(
                "}");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "IMPORTANT");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "- The \"answers\" array MUST contain exactly one object.");

            resultBuilder.AppendLine(
                "- Process the provided information need.");

            resultBuilder.AppendLine(
                "- Do not omit the provided information need.");

            resultBuilder.AppendLine(
                "- Replace all placeholders with actual values from " +
                "the provided EVIDENCE UNITS.");

            resultBuilder.AppendLine(
                "- Never invent result indexes.");

            resultBuilder.AppendLine(
                "- Never invent evidence indexes.");

            resultBuilder.AppendLine(
                "- Return ONLY valid JSON.");

            resultBuilder.AppendLine(
                "- Do not return evidence text.");

            resultBuilder.AppendLine(
                "- Do not return explanations.");

            resultBuilder.AppendLine(
                "- Do not answer the information need.");

            messages.Add(
                new OllamaChatMessage
                {
                    Role = "system",
                    Content = resultBuilder.ToString()
                });

            Console.WriteLine("========= EVIDENCE SELECTION PROMPT =========");

            foreach (var message in messages)
            {
                Console.WriteLine($"ROLE: {message.Role}");
                Console.WriteLine(message.Content);
                Console.WriteLine("---------------------------------------------");
            }

            Console.WriteLine(
                "=============================================");

            return new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,
                Messages = messages,

                Format = new
                {
                    type = "object",
                    properties = new
                    {
                        answers = new
                        {
                            type = "array",
                            minItems = 1,
                            maxItems = 1,
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    informationNeed = new
                                    {
                                        type = "string"
                                    },
                                    evidence = new
                                    {
                                        type = "array",
                                        minItems = 0,
                                        items = new
                                        {
                                            type = "object",
                                            properties = new
                                            {
                                                resultIndex = new
                                                {
                                                    type = "integer"
                                                },
                                                evidenceIndexes = new
                                                {
                                                    type = "array",
                                                    minItems = 1,
                                                    items = new
                                                    {
                                                        type = "integer"
                                                    }
                                                }
                                            },
                                            required = new[]
                                            {
                                                "resultIndex",
                                                "evidenceIndexes"
                                            },
                                            additionalProperties = false
                                        }
                                    },
                                    complete = new
                                    {
                                        type = "boolean"
                                    }
                                },
                                required = new[]
                                {
                                    "informationNeed",
                                    "evidence",
                                    "complete"
                                },
                                additionalProperties = false
                            }
                        }
                    },
                    required = new[]
                    {
                        "answers"
                    },
                    additionalProperties = false
                },

                Options = new OllamaGenerationOptions
                {
                    Temperature = 0,
                    Seed = 42
                }
            };
        }
     public OllamaChatRequest CreateCorrectiveEvidenceSelectionRequest(
     ChatRequest request,
     IReadOnlyList<EvidenceUnit> evidenceUnits,
     string informationNeed,
     IReadOnlyList<EvidenceUnit> previouslySelectedEvidence)
        {
            var resultBuilder = new StringBuilder();

            resultBuilder.AppendLine(
                "You are a corrective evidence selector for a generic knowledge retrieval system.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "Your task is to select additional evidence units that may complete " +
                "an information need when the previously selected evidence was insufficient.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "The information need may refer to ANY subject, domain, document, person, " +
                "technology, process, product, organization, or other topic.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "STRICT RULES");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "1. Use ONLY the evidence units provided below.");

            resultBuilder.AppendLine(
                "2. Do not use pretrained knowledge, outside information, assumptions, " +
                "or common knowledge.");

            resultBuilder.AppendLine(
                "3. Preserve the original information need exactly.");

            resultBuilder.AppendLine(
                "4. Select evidence that provides missing information required to " +
                "support the complete information need.");

            resultBuilder.AppendLine(
                "5. Prefer evidence that complements the previously selected evidence.");

            resultBuilder.AppendLine(
                "6. Do not select evidence merely because it mentions the same subject.");

            resultBuilder.AppendLine(
                "7. If the information need asks for a relationship, comparison, " +
                "connection, dependency, interaction, or other multi-part concept, " +
                "select evidence that supports the requested concept itself.");

            resultBuilder.AppendLine(
                "8. Do not remove previously selected evidence.");

            resultBuilder.AppendLine(
                "9. Do not invent evidence references.");

            resultBuilder.AppendLine(
                "10. Do not answer the information need.");

            resultBuilder.AppendLine(
                "11. Do not return evidence text.");

            resultBuilder.AppendLine(
                "12. Return only evidence references.");

            resultBuilder.AppendLine(
                "13. Select the smallest set of additional evidence needed.");

            resultBuilder.AppendLine(
                "14. If no additional evidence can improve completeness, " +
                "return an empty evidence array.");

            resultBuilder.AppendLine(
                "15. The complete field must always be false. " +
                "Completeness will be evaluated separately.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "INFORMATION NEED");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                informationNeed);

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "PREVIOUSLY SELECTED EVIDENCE");

            resultBuilder.AppendLine(
                "========================================");

            foreach (var evidence in previouslySelectedEvidence)
            {
                resultBuilder.AppendLine(
                    $"[Result {evidence.ResultIndex}] " +
                    $"[Evidence {evidence.EvidenceIndex}] " +
                    $"{evidence.Text}");
            }

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "AVAILABLE EVIDENCE");

            resultBuilder.AppendLine(
                "========================================");

            foreach (var evidence in evidenceUnits)
            {
                resultBuilder.AppendLine(
                    $"[Result {evidence.ResultIndex}] " +
                    $"[Evidence {evidence.EvidenceIndex}] " +
                    $"{evidence.Text}");
            }

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "OUTPUT FORMAT");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                """
                    Return ONLY valid JSON using this structure:

                    {
                      "answers": [
                        {
                          "informationNeed": "<exact information need>",
                          "evidence": [
                            {
                              "resultIndex": <actual result index>,
                              "evidenceIndexes": [<actual evidence index>]
                            }
                          ],
                          "complete": false
                        }
                      ]
                    }

                    IMPORTANT:
                    - The "complete" field must always be false.
                    - Do not use the "complete" field to decide whether the information need
                      is complete.
                    - Completeness will be evaluated separately after corrective evidence
                      selection.
                    - Your task is only to identify additional evidence that may fill
                      missing information.
                    """);

            return new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,

                Messages =
                [
                    new OllamaChatMessage
            {
                Role = "system",
                Content = resultBuilder.ToString()
            }
                ],

                Format = new
                {
                    type = "object",

                    properties = new
                    {
                        answers = new
                        {
                            type = "array",

                            items = new
                            {
                                type = "object",

                                properties = new
                                {
                                    informationNeed = new
                                    {
                                        type = "string"
                                    },

                                    evidence = new
                                    {
                                        type = "array",

                                        items = new
                                        {
                                            type = "object",

                                            properties = new
                                            {
                                                resultIndex = new
                                                {
                                                    type = "integer"
                                                },

                                                evidenceIndexes = new
                                                {
                                                    type = "array",

                                                    items = new
                                                    {
                                                        type = "integer"
                                                    }
                                                }
                                            },

                                            required = new[]
                                            {
                                        "resultIndex",
                                        "evidenceIndexes"
                                    },

                                            additionalProperties = false
                                        }
                                    },

                                    complete = new
                                    {
                                        type = "boolean"
                                    }
                                },

                                required = new[]
                                {
                            "informationNeed",
                            "evidence",
                            "complete"
                        },

                                additionalProperties = false
                            }
                        }
                    },

                    required = new[]
                    {
                "answers"
            },

                    additionalProperties = false
                },

                Options = new OllamaGenerationOptions
                {
                    Temperature = 0,
                    Seed = 42
                }
            };
        }

    }
}
