namespace AIChatAssistant.Models.Tools
{
    public class ToolDefinition
    {
        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public IReadOnlyList<string> Parameters { get; init; } = [];

        public IReadOnlyList<string> WhenToUse { get; init; } = [];

        public IReadOnlyList<string> Examples { get; init; } = [];
    }
}
