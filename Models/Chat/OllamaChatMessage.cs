using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.Chat
{
    public class OllamaChatMessage
    {

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
