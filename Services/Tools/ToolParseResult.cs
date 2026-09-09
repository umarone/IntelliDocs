using AIChatAssistant.Enums;
using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Services.Tools
{
    public class ToolParseResult
    {
        public ToolParseStatus Status { get; init; }

        public IReadOnlyList<ToolInvocation> ToolInvocations { get; init; }
            = [];

        public string? Error { get; init; }

    }
}
