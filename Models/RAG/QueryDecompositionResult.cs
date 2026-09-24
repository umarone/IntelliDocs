using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.RAG
{
    public class QueryDecompositionResult
    {
        [JsonPropertyName("queries")]
        public List<string> Queries { get; set; } = [];
    }
}
