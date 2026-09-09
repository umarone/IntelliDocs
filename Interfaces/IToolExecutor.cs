using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Interfaces
{
    public interface IToolExecutor
    {
        Task<ToolResult> ExecuteAsync(
        ToolCallRequest request);
    }
}
