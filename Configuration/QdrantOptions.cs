namespace AIChatAssistant.Configuration
{
    public class QdrantOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string CollectionName { get; set; } = string.Empty;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                throw new InvalidOperationException(
                    "Qdrant BaseUrl is not configured.");
            }

            if (!Uri.TryCreate(
                    BaseUrl,
                    UriKind.Absolute,
                    out var uri) ||
                uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException(
                    "Qdrant BaseUrl is invalid.");
            }

            if (string.IsNullOrWhiteSpace(CollectionName))
            {
                throw new InvalidOperationException(
                    "Qdrant CollectionName is not configured.");
            }
        }
    }
}
