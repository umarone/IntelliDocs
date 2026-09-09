using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Tools;
using System.Text.Json;
using AIChatAssistant.Enums;
namespace AIChatAssistant.Services.Tools
{
    public class ToolCallParser : IToolCallParser
    {
        public ToolParseResult Parse(
            string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new ToolParseResult()
                {
                    Status = ToolParseStatus.NoToolCall
                };
            }

            var results = new List<ToolInvocation>();
            
            var jsonObjects = ExtractJsonObjects(content).ToList();
            // No JSON object at all
            
            if (jsonObjects.Count == 0)
            {
                return new ToolParseResult
                {
                    Status = ToolParseStatus.NoToolCall
                };
            }
            foreach (var jsonObject in jsonObjects)
            {
                try
                {
                    using var document =
                        JsonDocument.Parse(jsonObject);

                    var root = document.RootElement;

                    if (!root.TryGetProperty(
                            "tool",
                            out var toolProperty))
                    {
                        continue;
                    }

                    if (toolProperty.ValueKind !=
                        JsonValueKind.String)
                    {
                        continue;
                    }

                    var toolName = toolProperty.GetString();

                    if (string.IsNullOrWhiteSpace(toolName))
                    {
                        continue;
                    }

                    var invocation = new ToolInvocation
                    {
                        Tool = toolName
                    };

                    if (root.TryGetProperty(
                            "arguments",
                            out var argumentsProperty) &&
                        argumentsProperty.ValueKind ==
                        JsonValueKind.Object)
                    {
                        foreach (var property in
                                 argumentsProperty.EnumerateObject())
                        {
                            invocation.Arguments[property.Name] =
                                property.Value.ValueKind ==
                                JsonValueKind.String
                                    ? property.Value.GetString() ?? string.Empty
                                    : property.Value.ToString();
                        }
                    }

                    results.Add(invocation);
                }
                catch (JsonException)
                {
                    // Ignore malformed JSON objects and
                    // continue looking for valid tool calls.
                    continue;
                }
            }

            if (results.Count == 0)
            {
                return new ToolParseResult
                {
                    Status = ToolParseStatus.NoToolCall
                };
            }
            Console.WriteLine($"Tools detected = {results.Count}");
            
            foreach (var invocation in results)
            {
                Console.WriteLine(
                    $"Tool = '{invocation.Tool}'");

                Console.WriteLine(
                    $"Arguments Count = {invocation.Arguments.Count}");
            }

            return new ToolParseResult
            {
                Status = ToolParseStatus.Success,
                ToolInvocations = results
            };
        }

        private static IEnumerable<string> ExtractJsonObjects(
            string content)
        {
            var objects = new List<string>();

            var depth = 0;
            var startIndex = -1;
            var insideString = false;
            var escaped = false;

            for (var i = 0; i < content.Length; i++)
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
                    if (depth == 0)
                    {
                        startIndex = i;
                    }

                    depth++;
                }
                else if (character == '}')
                {
                    if (depth == 0)
                    {
                        continue;
                    }

                    depth--;

                    if (depth == 0 && startIndex >= 0)
                    {
                        objects.Add(
                            content.Substring(
                                startIndex,
                                i - startIndex + 1));

                        startIndex = -1;
                    }
                }
            }

            return objects;
        }
    }
}