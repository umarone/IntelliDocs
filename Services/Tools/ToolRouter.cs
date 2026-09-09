using AIChatAssistant.Common;
using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.Tools;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services.Tools
{
    public class ToolRouter : IToolRouter
    {
        private readonly IEnumerable<ITool> _tools;
        private readonly IOllamaClient _ollamaClient;
        private readonly OllamaOptions _options;

        public ToolRouter(
            IEnumerable<ITool> tools,
            IOllamaClient ollamaClient,
            IOptions<OllamaOptions> options)
        {
            _tools = tools;
            _ollamaClient = ollamaClient;
            _options = options.Value;
        }

        public async Task<ToolRoutingDecision> DecideAsync(
    ChatRequest request,
    ToolRoutingContext? context)
        {
            var latestUserMessage =
                request.Messages.LastOrDefault(m =>
                    string.Equals(
                        m.Role,
                        "user",
                        StringComparison.OrdinalIgnoreCase));

            if (latestUserMessage is null)
            {
                throw new InvalidOperationException(
                    "Chat request does not contain a user message.");
            }

            var prompt =
                BuildRoutingPrompt(
                    request,
                    context);

            var ollamaRequest = new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,
                Messages =
                [
                    new OllamaChatMessage
            {
                Role = "system",
                Content = prompt
            },
            new OllamaChatMessage
            {
                Role = "user",
                Content = latestUserMessage.Content
            }
                ]
            };

            const int maxAttempts = 2;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var response =
                        await _ollamaClient.SendAsync(
                            ollamaRequest);

                    Console.WriteLine(
                        "========= TOOL ROUTER RESPONSE =========");

                    Console.WriteLine(
                        response.Message.Content);

                    Console.WriteLine(
                        "=========================================");

                    var decision =
                        ParseDecision(
                            response.Message.Content);

                    ValidateDecision(
                        decision);

                    return decision;
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(
                        $"Tool router attempt {attempt} failed: " +
                        $"{ex.Message}");

                    if (attempt == maxAttempts)
                    {
                        throw;
                    }
                }
            }

            throw new InvalidOperationException(
                "Tool router failed to produce a valid routing decision.");
        }

        private string BuildRoutingPrompt(ChatRequest request, ToolRoutingContext? context)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine(
                "You are an AI tool routing system.");

            prompt.AppendLine();

            prompt.AppendLine(
                "Your job is to understand the user's request and decide " +
                "whether one of the available tools is genuinely required.");

            prompt.AppendLine();

            prompt.AppendLine("IMPORTANT DECISION RULES");
            prompt.AppendLine("-----------------------");

            prompt.AppendLine(
                "1. Understand the user's actual intent before selecting a tool.");

            prompt.AppendLine(
                "2. Use a tool ONLY when the tool is genuinely required " +
                "to answer the user's request.");

            prompt.AppendLine(
                "3. If the user is having a normal conversation, greeting you, " +
                "asking how you are, making small talk, asking for a joke, " +
                "or asking for an opinion, DO NOT use any tool.");

            prompt.AppendLine(
                "4. If you can answer the user's request normally without a tool, " +
                "return useTool=false.");

            prompt.AppendLine(
                "5. Do NOT use a tool merely because the tool could provide " +
                "additional information.");

            prompt.AppendLine(
                "6. Use Calculator when the user actually requires a " +
                "mathematical calculation or numerical result.");

            prompt.AppendLine(
                "7. Use CurrentDate ONLY when the user asks for today's date, " +
                "the current date, or what day/date it is.");

            prompt.AppendLine(
                "8. Use KnowledgeSearch when the answer should come from " +
                "the application's knowledge base or stored documents.");

            prompt.AppendLine(
                "9. Do NOT use KnowledgeSearch for greetings, casual conversation, " +
                "jokes, opinions, or questions that do not require the knowledge base.");

            prompt.AppendLine(
                "10. When a tool is required, select exactly one tool.");

            prompt.AppendLine(
                "11. When no tool is genuinely required, return useTool=false.");
            prompt.AppendLine(
                "12. If the user's request has already been completely " +
                "answered by a previous successful tool result, return " +
                "useTool=false.");
            prompt.AppendLine(
                "13. Do NOT execute the same tool again when its previous " +
                "result already satisfies the user's request.");

            prompt.AppendLine();
            //prompt.AppendLine("ORIGINAL USER REQUEST");
            //prompt.AppendLine("---------------------");
            //prompt.AppendLine(
            //    request.Messages.Last().Content);
            //prompt.AppendLine();

            prompt.AppendLine("PREVIOUS TOOL CONTEXT");
            prompt.AppendLine("---------------------");

            prompt.AppendLine(
                $"Previous tool: {context?.PreviousToolName ?? "None"}");

            prompt.AppendLine(
                $"Previous tool result: {context?.PreviousToolResult ?? "None"}");

            prompt.AppendLine();

            prompt.AppendLine("AVAILABLE TOOLS");
            prompt.AppendLine("----------------");

            foreach (var tool in _tools)
            {
                var definition = tool.Definition;

                prompt.AppendLine(
                    $"Tool: {definition.Name}");

                prompt.AppendLine(
                    $"Description: {definition.Description}");

                prompt.AppendLine("Parameters:");

                foreach (var parameter in definition.Parameters)
                {
                    prompt.AppendLine(
                        $"- {parameter}");
                }

                prompt.AppendLine("When to use:");

                foreach (var scenario in definition.WhenToUse)
                {
                    prompt.AppendLine(
                        $"- {scenario}");
                }

                prompt.AppendLine();
            }

            prompt.AppendLine("EXAMPLES");
            prompt.AppendLine("----------------");

            prompt.AppendLine(
                "User: Hello, how are you?");

            prompt.AppendLine(
                """Response: {"useTool":false,"toolName":null,"arguments":{}}""");

            prompt.AppendLine();

            prompt.AppendLine(
                "User: What is 2 + 2?");

            prompt.AppendLine(
                """Response: {"useTool":true,"toolName":"Calculator","arguments":{"expression":"2 + 2"}}""");

            prompt.AppendLine();

            prompt.AppendLine(
                "User: What is today's date?");

            prompt.AppendLine(
                """Response: {"useTool":true,"toolName":"CurrentDate","arguments":{}}""");

            prompt.AppendLine();

            prompt.AppendLine(
                "User: What is JWT?");

            prompt.AppendLine(
                """Response: {"useTool":true,"toolName":"KnowledgeSearch","arguments":{"query":"JWT"}}""");

            prompt.AppendLine();

            prompt.AppendLine(
                "User: Tell me a joke.");

            prompt.AppendLine(
                """Response: {"useTool":false,"toolName":null,"arguments":{}}""");

            prompt.AppendLine();

            prompt.AppendLine("OUTPUT RULES");
            prompt.AppendLine("----------------");

            prompt.AppendLine(
                "Return ONLY valid JSON.");

            prompt.AppendLine(
                "Do NOT return markdown.");

            prompt.AppendLine(
                "Do NOT return explanations.");

            prompt.AppendLine(
                "Do NOT include ```json or ```.");

            prompt.AppendLine(
                "Do NOT include any text before or after the JSON.");

            prompt.AppendLine();

            prompt.AppendLine("JSON FORMAT");
            prompt.AppendLine("----------------");

            prompt.AppendLine(
                """
                {
                  "useTool": false,
                  "toolName": null,
                  "arguments": {}
                }
            """);

            return prompt.ToString();
        }

        private static ToolRoutingDecision ParseDecision(string content)
        {
            try
            {
                var json = ExtractJsonObject(content);

                if (json is null)
                {
                    throw new InvalidOperationException("The tool router did not return a valid JSON object.");
                }

                using var document = JsonDocument.Parse(json);

                var root = document.RootElement;

                if (!root.TryGetProperty("useTool",out var useToolProperty) || (useToolProperty.ValueKind != JsonValueKind.True && useToolProperty.ValueKind != JsonValueKind.False))
                {
                    throw new InvalidOperationException(
                        "The tool router response is missing a valid 'useTool' property.");
                }

                var useTool =
                    useToolProperty.GetBoolean();

                string? toolName = null;

                if (root.TryGetProperty(
                        "toolName",
                        out var toolNameProperty) &&
                    toolNameProperty.ValueKind ==
                    JsonValueKind.String)
                {
                    toolName = toolNameProperty.GetString();
                }

                var arguments =
                    new Dictionary<string, string>();

                if (root.TryGetProperty(
                        "arguments",
                        out var argumentsProperty) &&
                    argumentsProperty.ValueKind ==
                    JsonValueKind.Object)
                {
                    foreach (var property in
                             argumentsProperty.EnumerateObject())
                    {
                        arguments[property.Name] =
                            property.Value.ToString();
                    }
                }

                return new ToolRoutingDecision
                {
                    UseTool = useTool,
                    ToolName = toolName,
                    Arguments = arguments
                };
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("The tool router returned invalid JSON.",ex);
            }
        }
        private void ValidateDecision(
    ToolRoutingDecision decision)
        {
            // No tool selected — nothing else to validate.
            if (!decision.UseTool)
            {
                if (!string.IsNullOrWhiteSpace(decision.ToolName))
                {
                    throw new InvalidOperationException(
                        "Router returned UseTool=false but also provided a tool name.");
                }

                if (decision.Arguments.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Router returned UseTool=false but also provided arguments.");
                }

                return;
            }

            // A tool was selected, so ToolName is mandatory.
            if (string.IsNullOrWhiteSpace(decision.ToolName))
            {
                throw new InvalidOperationException(
                    "Router selected a tool but did not provide a tool name.");
            }

            // The selected tool must actually exist.
            var selectedTool = _tools.FirstOrDefault(
                x => string.Equals(
                    x.Definition.Name,
                    decision.ToolName,
                    StringComparison.OrdinalIgnoreCase));

            if (selectedTool is null)
            {
                throw new InvalidOperationException(
                    $"Router selected unknown tool '{decision.ToolName}'.");
            }

            // Validate required parameters.
            foreach (var parameter in selectedTool.Definition.Parameters)
            {
                if (!decision.Arguments.TryGetValue(
                        parameter,
                        out var value) ||
                    string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException(
                        $"Tool '{selectedTool.Definition.Name}' " +
                        $"requires parameter '{parameter}'.");
                }
            }

            // Validate that router did not invent parameters.
            foreach (var argument in decision.Arguments.Keys)
            {
                if (!selectedTool.Definition.Parameters.Any(
                        p => string.Equals(
                            p,
                            argument,
                            StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        $"Tool '{selectedTool.Definition.Name}' " +
                        $"does not support parameter '{argument}'.");
                }
            }
        }
        private static string? ExtractJsonObject(string content)
        {
            var start = content.IndexOf('{');

            if (start < 0)
            {
                return null;
            }

            var depth = 0;
            var insideString = false;
            var escaped = false;

            for (var i = start; i < content.Length; i++)
            {
                var character = content[i];

                if (insideString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (character == '\\')
                    {
                        escaped = true;
                    }
                    else if (character == '"')
                    {
                        insideString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    insideString = true;
                    continue;
                }

                if (character == '{')
                {
                    depth++;
                }
                else if (character == '}')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return content.Substring(
                            start,
                            i - start + 1);
                    }
                }
            }

            return null;
        }
    }
}