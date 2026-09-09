using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.AI.Providers.Ollama
{
    public class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embeddings")]
        public List<float[]> Embeddings { get; set; } = [];
    }
}
