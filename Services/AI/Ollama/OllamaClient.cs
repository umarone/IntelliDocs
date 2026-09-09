using AIChatAssistant.Common;
using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.Ollama
{
    public class OllamaClient : IOllamaClient
    {
        private readonly OllamaOptions _options;
        private readonly HttpClient _httpClient;
        public OllamaClient(HttpClient httpClient, IOptions<OllamaOptions> options)
        {
            _options = options.Value;
            _httpClient = httpClient;
        }
        public async Task<OllamaChatResponse> SendAsync(
    OllamaChatRequest request)
        {
            var json = JsonSerializer.Serialize(
                request,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            Console.WriteLine(
                "========= JSON SENT TO OLLAMA =========");

            Console.WriteLine(json);

            Console.WriteLine(
                "=======================================");

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"{_options.BaseUrl}/api/chat",
                content);

            // Read response body BEFORE EnsureSuccessStatusCode()
            var responseJson =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                "========= HTTP STATUS =========");

            Console.WriteLine(
                $"{(int)response.StatusCode} - {response.StatusCode}");

            Console.WriteLine(
                "===============================");

            Console.WriteLine(
                "========= JSON RECEIVED FROM OLLAMA =========");

            Console.WriteLine(responseJson);

            Console.WriteLine(
                "=============================================");

            // Now throw if Ollama returned an error
            response.EnsureSuccessStatusCode();

            var result =
                JsonSerializer.Deserialize<OllamaChatResponse>(
                    responseJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null)
            {
                throw new InvalidOperationException(
                    "Invalid response received from Ollama.");
            }

            result.Message.Content =
                ResponseCleaner.Clean(
                    result.Message.Content);

            return result;
        }

        public async IAsyncEnumerable<StreamingChatChunk> StreamAsync(OllamaChatRequest request)
        {
            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl}/api/chat");

            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();

            using var reader = new StreamReader(stream);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {

                    continue;
                }
                OllamaChatResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
                }
                catch (JsonException)
                {
                    continue;
                }
                if (chunk == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(chunk.Message?.Content))
                {
                    yield return new StreamingChatChunk
                    {
                        Content = chunk.Message.Content,
                        IsCompleted = chunk.Done
                    };
                }
            }
        }
    }
}
