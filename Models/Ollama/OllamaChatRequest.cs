using AIChatAssistant.Configuration;
using AIChatAssistant.Models.Chat;
using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.Ollama
{
    public class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; }
       
        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("format")]
        public object? Format { get; set; }

        [JsonPropertyName("options")]
        public OllamaGenerationOptions? Options { get; set; }
    }
}
