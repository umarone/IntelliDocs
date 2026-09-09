using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Interfaces
{
    public interface ITool
    {
        ToolDefinition Definition { get; }
        //IReadOnlyList<string> Parameters { get; }
        Task<ToolResult> ExecuteAsync(
            ToolCallRequest request);
    }
}
