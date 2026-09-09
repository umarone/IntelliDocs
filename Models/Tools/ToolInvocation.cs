using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.Tools
{
    public class ToolInvocation
    {
        //[JsonPropertyName("tool")]
        public string Tool { get; set; } = string.Empty;
       // [JsonPropertyName("arguments")]
        public Dictionary<string, string> Arguments { get; set; }
            = new();
    }
}
