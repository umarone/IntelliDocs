namespace AIChatAssistant.Models.Tools
{
    public class ToolResult
    {
        public string ToolName { get; init; } = string.Empty;
        public bool Success { get; init; }

        public string Data { get; init; } = string.Empty;

        public string? Error { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }
    }
}
