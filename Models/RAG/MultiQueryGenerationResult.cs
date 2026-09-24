using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.RAG
{
    public class MultiQueryGenerationResult
    {
        [JsonPropertyName("queries")]
        public List<string> Queries { get; set; } = [];
    }
}
