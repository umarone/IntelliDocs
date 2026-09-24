using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.Quadrant
{
    public class QdrantPayloadIndexRequest
    {
        [JsonPropertyName("field_name")]
        public string FieldName { get; set; } = string.Empty;

        [JsonPropertyName("field_schema")]
        public string FieldSchema { get; set; } = "keyword";
    }
}
