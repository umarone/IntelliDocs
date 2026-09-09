using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Services.Conversation
{
    public class ConversationContextSelector
        : IConversationContextSelector
    {
        private static readonly string[] FollowUpIndicators =
        [
            "it",
            "its",
            "this",
            "that",
            "these",
            "those",
            "they",
            "them",
            "he",
            "she",
            "how does it work",
            "how does this work",
            "explain that",
            "explain this",
            "tell me more",
            "what about",
            "what are its",
            "what are the advantages",
            "what are the disadvantages",
            "why is that",
            "why does it",
            "how about"
        ];

        public IReadOnlyList<ChatMessage> SelectRelevantHistory(
            IReadOnlyList<ChatMessage> history,
            ChatMessage currentMessage)
        {
            if (history.Count == 0)
            {
                return [];
            }

            if (string.IsNullOrWhiteSpace(currentMessage.Content))
            {
                return [];
            }

            // Remove internal tool-call messages from conversational history.
            var conversationalHistory = history
                .Where(IsConversationalMessage)
                .ToList();

            if (conversationalHistory.Count == 0)
            {
                return [];
            }

            var question =
                currentMessage.Content
                    .Trim()
                    .ToLowerInvariant();

            var isFollowUp = FollowUpIndicators.Any(indicator =>
                question.Contains(indicator));

            if (!isFollowUp)
            {
                return [];
            }

            return conversationalHistory;
        }

        private static bool IsConversationalMessage(
            ChatMessage message)
        {
            if (string.IsNullOrWhiteSpace(message.Content))
            {
                return false;
            }

            // User messages are always conversational.
            if (message.Role.Equals(
                "user",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Assistant messages are conversational unless
            // they contain an internal tool-call JSON.
            if (message.Role.Equals(
                "assistant",
                StringComparison.OrdinalIgnoreCase))
            {
                var content = message.Content.Trim();

                if (content.StartsWith("{") &&
                    content.Contains(
                        "\"tool\"",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }

            // System messages are internal instructions/results,
            // so don't include them in conversational memory.
            return false;
        }
    }
}
