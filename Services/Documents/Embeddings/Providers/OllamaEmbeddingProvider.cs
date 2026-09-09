using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Embeddings;
using AIChatAssistant.Models.AI.Providers.Ollama;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services.Documents.Embeddings.Providers
{
    public class OllamaEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaOptions _options;

        public OllamaEmbeddingProvider(
            HttpClient httpClient,
            IOptions<OllamaOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        EmbeddingRequest request)
        {
            var ollamaRequest = BuildOllamaRequest(request);

            var ollamaResponse = await SendToOllamaAsync(ollamaRequest);

            return MapResponse(request, ollamaResponse);
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var request = new OllamaEmbeddingRequest
            {
                Model = _options.EmbeddingModel,
                Input = new List<string> { text }
            };

            var response = await SendToOllamaAsync(request);

            return response.Embeddings.First();
        }
        //    private List<OllamaChatMessage> BuildMessages(
        //ChatRequest request)
        //    {
        //        var history = _conversationService
        //   .GetMessages(request.ConversationId)
        //   .Select(m => new OllamaChatMessage
        //   {
        //       Role = m.Role,
        //       Content = m.Content
        //   })
        //   .ToList();

        //        if (!string.IsNullOrWhiteSpace(request.Prompt))
        //        {
        //            history.Add(new OllamaChatMessage
        //            {
        //                Role = "user",
        //                Content = request.Prompt
        //            });
        //        }

        //        return history;
        //    }
        private OllamaEmbeddingRequest BuildOllamaRequest(
         EmbeddingRequest request)
        {
            return new OllamaEmbeddingRequest
            {
                Model = _options.EmbeddingModel,
                Input = request.Chunks
                    .Select(c => c.Content)
                    .ToList()
            };
        }
        private async Task<OllamaEmbeddingResponse> SendToOllamaAsync(
    OllamaEmbeddingRequest request)
        {
            var json = JsonSerializer.Serialize(request);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/api/embed",
                content);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(
                responseJson);

            if (result == null)
            {
                throw new InvalidOperationException(
                    "Invalid response received from Ollama.");
            }

            return result;
        }
        private EmbeddingResponse MapResponse(
    EmbeddingRequest request,
    OllamaEmbeddingResponse response)
        {
            if (request.Chunks.Count != response.Embeddings.Count)
            {
                throw new InvalidOperationException(
                    "The number of embeddings returned by Ollama does not match the number of input chunks.");
            }
            var result = new EmbeddingResponse();

            for (int i = 0; i < request.Chunks.Count; i++)
            {
                result.Embeddings.Add(new ChunkEmbedding
                {
                    ChunkId = request.Chunks[i].Id,
                    Vector = response.Embeddings[i]
                });
            }

            return result;
        }
    }
}
