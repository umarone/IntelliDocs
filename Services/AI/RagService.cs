using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.VectorStore;
using AIChatAssistant.Models.Chat;
using System.Text;

namespace AIChatAssistant.Services.AI
{
    public class RagService : IRagService
    {
        private readonly IDocumentSearchService _documentSearchService;
        private readonly IChatProvider _chatProvider;
        private readonly IPromptBuilder _promptBuilder;
        public RagService(
            IDocumentSearchService documentSearchService,
            IChatProvider chatProvider,
            IPromptBuilder promptBuilder)
        {
            _documentSearchService = documentSearchService;
            _chatProvider = chatProvider;
            _promptBuilder = promptBuilder;
        }

        public async Task<ChatResponse> AskAsync(ChatRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var question = request.Messages
       .LastOrDefault(m => m.Role == "user")?.Content;
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException(
                    "Prompt cannot be empty.",
                    nameof(request));
            }
            Console.WriteLine("========== Search Results ==========");

            Console.WriteLine(
     $"Filter FileName: {request.Filter?.FileName ?? "<null>"}");

            Console.WriteLine(
                $"Filter DocumentId: {request.Filter?.DocumentId?.ToString() ?? "<null>"}");

            var chunks = await _documentSearchService.SearchAsync(question,request.Filter);

            if (!chunks.Any())
            {
                return new ChatResponse
                {
                    Message = new ChatMessage
                    {
                        Role = "assistant",
                        Content = "I couldn't find that information in the uploaded documents."
                    }
                };
            }

            Console.WriteLine("========== Search Results ==========");

            foreach (var chunk in chunks)
            {
                Console.WriteLine($"Rank       : {chunk.Rank}");
                Console.WriteLine($"Similarity : {chunk.Similarity:F4}");
                Console.WriteLine($"Content    :");
                Console.WriteLine(chunk.Record.Content);
                Console.WriteLine("------------------------------------");
            }

            var prompt = _promptBuilder.BuildPrompt(chunks, question);

            Console.WriteLine("========== PROMPT ==========");
            Console.WriteLine(prompt);
            Console.WriteLine("============================");

            var response =  await AskModelAsync(request, prompt);
            response.Sources = chunks.Select(chunk => new SourceReference { 
                Rank = chunk.Rank,
                Similarity = chunk.Similarity,
                DocumentId = chunk.Record.DocumentId,
                ChunkIndex = chunk.Record.ChunkIndex,
                FileName = chunk.Record.FileName,
                ContentLength = chunk.Record.Content.Length
            }).ToList();

            return response;
        }
        private string BuildPrompt(
    List<VectorRecord> chunks,
    string question)
        {
            var context = string.Join(
       Environment.NewLine + Environment.NewLine,
       chunks.Select(c => c.Content));

            return $@"
            You are an AI assistant.

            Use ONLY the information provided in the CONTEXT below.

            Rules:
            - Answer ONLY using the CONTEXT.
            - If the answer exists in the CONTEXT, answer it directly.
            - Do NOT say you couldn't find the answer if it is clearly present.
            - If the answer is not in the CONTEXT, reply exactly:
            I couldn't find that information in the uploaded documents.

            CONTEXT:
            {context}

            QUESTION:
            {question}

            ANSWER:";
        }
        private string BuildPrompt111(
    List<VectorRecord> chunks,
    string question)
        {
            var builder = new StringBuilder();

            builder.AppendLine("You are a helpful AI assistant.");

            builder.AppendLine();

            builder.AppendLine("Answer the user's question using ONLY the provided context.");

            builder.AppendLine();

            builder.AppendLine("If the answer is not present in the context, say:");
            builder.AppendLine("\"I couldn't find that information in the uploaded documents.\"");

            builder.AppendLine();

            builder.AppendLine("Context:");
            builder.AppendLine("----------------------------------------");

            foreach (var chunk in chunks)
            {
                builder.AppendLine(chunk.Content);
                builder.AppendLine();
            }

            builder.AppendLine("----------------------------------------");

            builder.AppendLine();

            builder.AppendLine($"Question: {question}");

            builder.AppendLine();

            builder.AppendLine("Answer:");

            return builder.ToString();
        }
        private async Task<ChatResponse> AskModelAsync(
    ChatRequest request,
    PromptMessages prompt)
        {
            var llmRequest = new ChatRequest
            {
                ConversationId = request.ConversationId,
                Messages = request.Messages
            .Take(request.Messages.Count - 1)
            .ToList()
            };
            llmRequest.Messages.Add(new ChatMessage
            {
                Role = "system",
                Content = prompt.SystemPrompt
            });
            llmRequest.Messages.Add(new ChatMessage
            {
                Role = "user",
                Content = prompt.UserPrompt
            });
            return await _chatProvider.ChatAsync(llmRequest);
        }
    }
}
