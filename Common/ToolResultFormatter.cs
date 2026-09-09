using AIChatAssistant.Models.Tools;
using System.Text;

namespace AIChatAssistant.Common
{
    public static class ToolResultFormatter
    {
        public static string Format(ToolResult toolResult)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Tool: {toolResult.ToolName}");
            if (!toolResult.Success)
            {
                builder.AppendLine("Status: Failed");
                builder.AppendLine($"Error: {toolResult.Error ?? "Unknown error."}");
                return builder.ToString().TrimEnd();
            }
            builder.AppendLine("Status: Success");
            builder.AppendLine($"Result: {toolResult.Data ?? string.Empty}"); return builder.ToString().TrimEnd();
        }
    }
}
