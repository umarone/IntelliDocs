using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Services.Tools
{
    public class ToolIntentDetector : IToolIntentDetector
    {
        private static readonly string[] Keywords =
        {
            "calculate",
            "plus",
            "minus",
            "multiply",
            "divide",
            "date",
            "today",
            "current date",

                 // Knowledge search
            "what is",
            "what are",
            "who is",
            "how does",
            "how do",
            "explain",
            "define",
            "tell me about"
        };
        public bool ShouldEnableTools(ChatRequest request)
        {
            var message = request.Messages.Last().Content.ToLowerInvariant();
            if (Keywords.Any(keyword => message.Contains(keyword)))
            {
                return true;
            }

            return ContainsMathExpression(message);

        }
        private static bool ContainsMathExpression(string message)
        {
            return message.Contains('+')
                || message.Contains('-')
                || message.Contains('*')
                || message.Contains('/')
                || message.Contains('%');
        }
    }
}
