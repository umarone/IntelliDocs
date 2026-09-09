using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Interfaces
{
    public interface IConversationContextSelector
    {
        IReadOnlyList<ChatMessage> SelectRelevantHistory(
            IReadOnlyList<ChatMessage> history,
            ChatMessage currentMessage);
    }

}
