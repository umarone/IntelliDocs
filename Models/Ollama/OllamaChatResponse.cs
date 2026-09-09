using AIChatAssistant.Models.Chat;
using System.Text.Json.Serialization;
namespace AIChatAssistant.Models.Ollama
{
    public class OllamaChatResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public OllamaChatMessage Message { get; set; } = new();

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
