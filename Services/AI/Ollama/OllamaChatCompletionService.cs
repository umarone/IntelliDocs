using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using Microsoft.Extensions.Options;

namespace AIChatAssistant.Services.AI.Ollama
{
    public class OllamaChatCompletionService : IChatCompletionService
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly OllamaOptions _options;

        public OllamaChatCompletionService(
            IOllamaClient ollamaClient,
            IOptions<OllamaOptions> options)
        {
            _ollamaClient = ollamaClient;
            _options = options.Value;
        }
        public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            var request =
             CreateRequest(
                 systemPrompt,
                 userPrompt);

            var response =
                await _ollamaClient.SendAsync(request);

            return response.Message.Content;
        }

        public async Task<string> CompleteStructuredAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
        {
            var request =
                CreateStructuredRequest(
                    systemPrompt,
                    userPrompt);

            var response =
                await _ollamaClient.SendAsync(request);

            return response.Message.Content;
        }

        private OllamaChatRequest CreateRequest(
        string systemPrompt,
        string userPrompt)
        {
            return new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,

                Messages =
                [
                    new OllamaChatMessage
                {
                    Role = "system",
                    Content = systemPrompt
                },

                new OllamaChatMessage
                {
                    Role = "user",
                    Content = userPrompt
                }
                ]
            };
        }
        private OllamaChatRequest CreateStructuredRequest(
        string systemPrompt,
        string userPrompt)
        {
            return new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,

                Messages =
                [
                    new OllamaChatMessage
                {
                    Role = "system",
                    Content = systemPrompt
                },

                new OllamaChatMessage
                {
                    Role = "user",
                    Content = userPrompt
                }
                ],

                Options =
                    new OllamaGenerationOptions
                    {
                        Temperature = 0,
                        Seed = 42
                    }
            };
        }
    }
}
