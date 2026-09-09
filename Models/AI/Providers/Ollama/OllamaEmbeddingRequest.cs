using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.AI.Providers.Ollama
{
    public class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = [];
    }
}
