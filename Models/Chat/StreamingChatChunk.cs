namespace AIChatAssistant.Models.Chat
{
    public class StreamingChatChunk
    {
        public string Content { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }
    }
}
