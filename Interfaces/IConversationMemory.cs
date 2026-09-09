using AIChatAssistant.Models.Chat;
using System.Threading.Tasks;

namespace AIChatAssistant.Interfaces
{
    public interface IConversationMemory
    {
        IReadOnlyList<ChatMessage> GetHistory(
    Guid conversationId,
    ChatMessage currentMessage);
    }
}
