using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Interfaces
{
    public interface ICalculatorService
    {
        Task<ToolResult> EvaluateAsync(string expression);
    }
}
