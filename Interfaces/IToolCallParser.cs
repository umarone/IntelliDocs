using AIChatAssistant.Models.Tools;
using AIChatAssistant.Services.Tools;

namespace AIChatAssistant.Interfaces
{
    public interface IToolCallParser
    {
        ToolParseResult Parse(string content);
    }
}
