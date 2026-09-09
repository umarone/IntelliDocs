namespace AIChatAssistant.Models.Tools
{
    public class ToolCallRequest
    {
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, object> Arguments { get; set; } = new();
        public string OriginalQuestion { get; set; } = string.Empty;
    }
}
