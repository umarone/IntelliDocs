using AIChatAssistant.Common;
using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.Tools;
using AIChatAssistant.Services.AI;
using AIChatAssistant.Services.Tools;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services.Providers.Chat
{
    public class OllamaChatProvider : IChatProvider
    {
        public string Name => "Ollama";
        private readonly IConversationService _conversationService;
        private readonly IToolExecutor _executor;
        private readonly IOllamaClient _ollamaClient;
        private readonly IOllamaPromptBuilder _promptBuilder;
        private readonly IAgentOrchestrator _agent;
        private readonly IKnowledgeRetriever _knowledgeRetriever;
        private readonly ISourceReferenceBuilder _sourceReferenceBuilder;
        public OllamaChatProvider(IConversationService conversationService, 
            IEnumerable<ITool> tools, IToolExecutor executor, IOllamaClient ollamaClient, 
            IOllamaPromptBuilder promptBuilder, IAgentOrchestrator agent, IKnowledgeRetriever knowledgeRetriever,
            ISourceReferenceBuilder sourceReferenceBuilder)
        {
            _ollamaClient = ollamaClient;
            _conversationService = conversationService;
            _executor = executor;
            _promptBuilder = promptBuilder;
            _agent = agent;
            _knowledgeRetriever = knowledgeRetriever;  
            _sourceReferenceBuilder = sourceReferenceBuilder;   
        }
        public async Task<ChatResponse> ChatAsync(ChatRequest request)
        {
            SaveUserMessage(request);
            var recentMessages = _conversationService.GetRecentMessages(request.ConversationId,10);
            var ollamaRequest = _promptBuilder.CreateChatRequest(request, []);

            //var response = await _agent.RunAsync(request, ollamaRequest);

            var response = await _agent.RunDocumentRagAsync(request, ollamaRequest);

            SaveAssistantMessage(request.ConversationId, response.Response);

            return new ChatResponse
            {
                Message = new ChatMessage
                {
                    Role = response.Response.Message.Role,
                    Content = response.Response.Message.Content
                },
                Sources = _sourceReferenceBuilder.Build(response.SearchResults) //SourceReferenceMapper.Map(searchResults)
            };
        }
        private void SaveUserMessage(ChatRequest request)
        {
            var latestMessage = request.Messages.Last();

            _conversationService.AddMessage(
                request.ConversationId,
                latestMessage);
        }
        private void SaveAssistantMessage(
    Guid conversationId,
    OllamaChatResponse response)
        {
            _conversationService.AddMessage(
                conversationId,
                new ChatMessage
                {
                    Role = "assistant",
                    Content = response.Message.Content
                });
        }
        private static ChatMessage GetLatestMessage(ChatRequest request)
        {
            return request.Messages.LastOrDefault()
                   ?? throw new InvalidOperationException(
                       "Chat request contains no messages.");
        }
        public async IAsyncEnumerable<StreamingChatChunk> StreamAsync(ChatRequest request)
        {
            var ollamaRequest = _promptBuilder.CreateChatRequest(request);

            ollamaRequest.Stream = true;
            await foreach (var chunk in _ollamaClient.StreamAsync(ollamaRequest))
            {
                // Optional processing
                yield return chunk;
            }
        }
    }
}
