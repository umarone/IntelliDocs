using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.Ollama
{
    public class OllamaGenerationOptions
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("seed")]
        public int Seed { get; set; }
    }
}
