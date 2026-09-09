namespace AIChatAssistant.Models.Tools
{
    public class ToolRoutingDecision
    {
        public bool UseTool { get; set; }

        public string? ToolName { get; set; }

        public Dictionary<string, string> Arguments { get; set; }
            = new();
    }
}
