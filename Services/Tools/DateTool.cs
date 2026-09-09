using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Services.Tools
{
    public class DateTool : ITool
    {
        public ToolDefinition Definition => new()
        {
            Name = "CurrentDate",

            Description = "Returns today's current date.",

            Parameters = [],

            WhenToUse =
            [
                "User asks today's date.",
                "User asks for the current date.",
                "User asks what day it is today."
            ],

            Examples =
            [
                "What is today's date?",
                "Tell me today's date.",
                "What day is it today?"
            ]
        };

        public Task<ToolResult> ExecuteAsync(ToolCallRequest request)
        {
            return Task.FromResult(new ToolResult { Success = true, Data =  DateTime.Now.ToString("yyyy-MM-dd") });
        }
    }
}
