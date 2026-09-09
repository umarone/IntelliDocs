namespace AIChatAssistant.Common
{
    public class ResponseCleaner
    {
        public static string Clean(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            response = response.Trim();

            if (response.StartsWith("assistant",
                StringComparison.OrdinalIgnoreCase))
            {
                response = response["assistant".Length..].Trim();
            }

            return response;
        }
    }
}
