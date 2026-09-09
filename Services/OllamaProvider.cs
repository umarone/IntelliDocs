
using AIChatAssistant.Configuration;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services
{
    public class OllamaProvider : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;
        private readonly IConversationService _conversationService;
        public OllamaProvider(HttpClient httpClient, IOptions<OllamaOptions> options, IConversationService conversationService)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _conversationService = conversationService;
        }
        public async Task<string> GetResponseAsync(ChatRequest request)
        {
            try
            {
                SaveUserMessage(request);

                var ollamaRequest = BuildOllamaRequest(request);

                var response = await SendToOllamaAsync(ollamaRequest);

                SaveAssistantMessage(request.ConversationId, response);

                return response.Message.Content;
            }
            catch (Exception ex) 
            {
                throw ex;
            }
        }
        private void SaveUserMessage(ChatRequest request)
        {
            var latestMessage = request.Messages.Last();

            _conversationService.AddMessage(
                request.ConversationId,
                latestMessage);
        }
        private OllamaChatRequest BuildOllamaRequest(ChatRequest request)
        {
            var history = _conversationService.GetMessages(
                request.ConversationId);

            return new OllamaChatRequest
            {
                Model = _options.Model,
                Messages = history,
                Stream = false
            };
        }
        private async Task<OllamaGenerateResponse> SendToOllamaAsync(
    OllamaChatRequest request)
        {
            var json = JsonSerializer.Serialize(request);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/api/chat",
                content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson);

            return result;
        }
        private void SaveAssistantMessage(
    Guid conversationId,
    OllamaGenerateResponse response)
        {
            _conversationService.AddMessage(
                conversationId,
                new ChatMessage
                {
                    Role = "assistant",
                    Content = response.Message.Content
                });
        }
    }
}
