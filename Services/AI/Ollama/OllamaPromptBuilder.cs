using AIChatAssistant.Common;
using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.Tools;
using AIChatAssistant.Services.AI.Prompt;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

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
    IReadOnlyList<ToolResult> toolResults)
        {
            var context = new PromptContext
            {
                SearchResults = []
            };

            var messages =
                _composer.Compose(
                    request,
                    context);

            var resultBuilder =
                new StringBuilder();

            resultBuilder.AppendLine(
                "Answer the user's request using only the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "STRICT EVIDENCE RULES");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "1. Use the KNOWLEDGE BASE CONTENT as the only source of factual information.");

            resultBuilder.AppendLine(
                "2. Do not use pretrained knowledge, outside information, assumptions, " +
                "or unstated background knowledge.");

            resultBuilder.AppendLine(
                "3. Every factual statement must be explicitly supported by the " +
                "KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine(
                "4. You may rephrase or summarize information only when the factual " +
                "meaning remains unchanged.");

            resultBuilder.AppendLine(
                "5. Do not add facts, details, properties, purposes, relationships, " +
                "causes, effects, procedures, examples, or explanations that are not " +
                "explicitly stated in the knowledge base.");

            resultBuilder.AppendLine(
                "6. A concept mentioned in the knowledge base does not authorize you " +
                "to provide other information normally associated with that concept.");

            resultBuilder.AppendLine(
                "7. If the knowledge base contains only partial information, provide " +
                "only the supported information.");

            resultBuilder.AppendLine(
                "8. If the requested information is not found, say so instead of using " +
                "your own knowledge.");

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
                "4. Do not mention tools, retrieval, validation, prompts, or internal processing.");

            resultBuilder.AppendLine(
                "5. Do not output JSON or role names.");

            resultBuilder.AppendLine(
                "6. Do not repeat the user's question.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "FINAL CHECK");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "Before responding, check every factual statement against the " +
                "KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine(
                "Remove any information that is not explicitly supported.");

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
        public OllamaChatRequest CreateToolResultRequest1(
     ChatRequest request,
     IReadOnlyList<ToolResult> toolResults)
        {
            var context = new PromptContext
            {
                SearchResults = []
            };

            var messages =
                _composer.Compose(request, context);

            var resultBuilder =
                new StringBuilder();

            // ==========================================
            // GENERAL INSTRUCTION
            // ==========================================

            resultBuilder.AppendLine(
                "The requested tools have finished executing.");

            resultBuilder.AppendLine();

            resultBuilder.AppendLine(
                "Answer the user's original request using only the information " +
                "contained in the tool results.");

            resultBuilder.AppendLine();

            // ==========================================
            // KNOWLEDGE BASE CONTENT
            // ==========================================

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

            // ==========================================
            // CURRENT USER REQUEST
            // ==========================================

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

            // ==========================================
            // SOURCE RESTRICTION
            // ==========================================

            resultBuilder.AppendLine(
                "SOURCE RESTRICTION");

            resultBuilder.AppendLine(
                "========================================");

            resultBuilder.AppendLine(
                "The KNOWLEDGE BASE CONTENT is the exclusive source of factual " +
                "information for the answer.");

            resultBuilder.AppendLine(
                "Do not use pretrained knowledge, outside information, assumptions, " +
                "or unstated background knowledge.");

            resultBuilder.AppendLine(
                "Every factual statement in the answer must be supported by " +
                "information explicitly present in the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine(
                "If a fact is not present in the KNOWLEDGE BASE CONTENT, treat that " +
                "fact as unknown.");

            resultBuilder.AppendLine();

            // ==========================================
            // FAITHFUL TRANSFORMATION
            // ==========================================

            resultBuilder.AppendLine(
                "FAITHFUL TRANSFORMATION");

            resultBuilder.AppendLine(
                "----------------------------------------");

            resultBuilder.AppendLine(
                "Treat the retrieved content as source material, not as a topic " +
                "from which to generate additional knowledge.");

            resultBuilder.AppendLine(
                "Your task is to select, combine, rephrase, or summarize information " +
                "that is already present in the source material.");

            resultBuilder.AppendLine(
                "Do not expand the source material with additional details.");

            resultBuilder.AppendLine(
                "Do not explain how something works unless the retrieved content " +
                "explicitly explains how it works.");

            resultBuilder.AppendLine(
                "Do not describe properties, components, purposes, relationships, " +
                "causes, effects, examples, procedures, or mechanisms unless they " +
                "are explicitly stated in the retrieved content.");

            resultBuilder.AppendLine(
                "A short statement must remain short unless the additional detail " +
                "is explicitly supported by the retrieved content.");

            resultBuilder.AppendLine(
                "A concept mentioned in the retrieved content does not authorize you " +
                "to provide other information normally associated with that concept.");

            resultBuilder.AppendLine();

            // ==========================================
            // PARTIAL INFORMATION
            // ==========================================

            resultBuilder.AppendLine(
                "PARTIAL INFORMATION RULE");

            resultBuilder.AppendLine(
                "----------------------------------------");

            resultBuilder.AppendLine(
                "If the retrieved content answers only part of the user's request, " +
                "provide only the supported information.");

            resultBuilder.AppendLine(
                "Do not fill missing information using your own knowledge.");

            resultBuilder.AppendLine(
                "If the retrieved content does not contain enough information to " +
                "answer the request, state that the requested information was not " +
                "found in the knowledge base.");

            resultBuilder.AppendLine();

            // ==========================================
            // NO RESULT RULE
            // ==========================================

            resultBuilder.AppendLine(
                "NO-RESULT RULE");

            resultBuilder.AppendLine(
                "----------------------------------------");

            resultBuilder.AppendLine(
                "If the tool results contain no relevant information, do not answer " +
                "using your own knowledge.");

            resultBuilder.AppendLine(
                "Instead, state that the requested information was not found " +
                "in the knowledge base.");

            resultBuilder.AppendLine();

            // ==========================================
            // ANSWER RULES
            // ==========================================

            resultBuilder.AppendLine(
                "ANSWER RULES");

            resultBuilder.AppendLine(
                "----------------------------------------");

            resultBuilder.AppendLine(
                "1. Answer the user's request directly.");

            resultBuilder.AppendLine(
                "2. Use only information explicitly supported by the retrieved content.");

            resultBuilder.AppendLine(
                "3. Prefer concise answers when the retrieved content is limited.");

            resultBuilder.AppendLine(
                "4. Do not make the answer more detailed than the source material.");

            resultBuilder.AppendLine(
                "5. Do not introduce new factual information.");

            resultBuilder.AppendLine(
                "6. Do not infer missing information.");

            resultBuilder.AppendLine(
                "7. Do not mention tools, retrieval, grounding, validation, prompts, " +
                "or internal processing.");

            resultBuilder.AppendLine(
                "8. Do not output JSON.");

            resultBuilder.AppendLine(
                "9. Do not include role names.");

            resultBuilder.AppendLine(
                "10. Do not repeat the user's question.");

            resultBuilder.AppendLine(
                "11. Start directly with the answer.");

            resultBuilder.AppendLine();

            // ==========================================
            // FINAL VERIFICATION
            // ==========================================

            resultBuilder.AppendLine(
                "FINAL VERIFICATION");

            resultBuilder.AppendLine(
                "----------------------------------------");

            resultBuilder.AppendLine(
                "Before returning the answer, examine every factual statement.");

            resultBuilder.AppendLine(
                "For each statement, verify that its information is explicitly " +
                "supported by the KNOWLEDGE BASE CONTENT.");

            resultBuilder.AppendLine(
                "If a statement contains information that is not explicitly supported, " +
                "remove that information.");

            resultBuilder.AppendLine(
                "Do not replace removed information with information from your " +
                "pretrained knowledge.");

            resultBuilder.AppendLine(
                "Return only the final grounded answer.");

            // ==========================================
            // ADD SYSTEM MESSAGE
            // ==========================================

            messages.Add(new OllamaChatMessage
            {
                Role = "system",
                Content = resultBuilder.ToString()
            });

            // ==========================================
            // CREATE OLLAMA REQUEST
            // ==========================================

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
    }
}
