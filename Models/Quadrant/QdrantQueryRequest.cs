using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.Quadrant
{
    public class QdrantQueryRequest
    {
        [JsonPropertyName("query")]
        public float[] Query { get; set; } = [];

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("filter")]
        public QdrantFilter? Filter { get; set; }

        [JsonPropertyName("with_payload")]
        public bool WithPayload { get; set; } = true;

        [JsonPropertyName("with_vector")]
        public bool WithVector { get; set; } = true;
    }
}
