using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using System.Text;

namespace AIChatAssistant.Services.AI.RagPromptBuilder
{
    public class RagPromptBuilder : IPromptBuilder
    {
        public PromptMessages BuildPrompt(
    List<SearchResult> chunks,
    string question)
        {
            var sb = new StringBuilder();

            sb.AppendLine("SOURCES:");
            sb.AppendLine();

            foreach (var chunk in chunks)
            {
                sb.AppendLine("==================================================");
                sb.AppendLine($"Source {chunk.Rank}");
                sb.AppendLine();
                sb.AppendLine($"File Name : {chunk.Record.FileName}");
                sb.AppendLine($"Rank      : {chunk.Rank}");
                sb.AppendLine($"Similarity: {chunk.Similarity:F2}");
                sb.AppendLine();
                sb.AppendLine("Content:");
                sb.AppendLine(chunk.Record.Content);
                sb.AppendLine("==================================================");
                sb.AppendLine();
            }

            return new PromptMessages
            {
                SystemPrompt =
                        """
                You are an enterprise AI assistant.

                Rules:

                - Answer ONLY using the supplied sources.
                - If multiple sources contain useful information, combine them into one answer.
                - Never invent facts.
                - Never use your own knowledge.
                - If the answer is not found in the sources, reply exactly:
                "I couldn't find that information in the uploaded documents."
                - Do NOT mention source numbers in the answer unless the user asks.
                - Keep the answer concise and professional.
                """,

                                UserPrompt =
                        $"""
                QUESTION:

                {question}

                {sb}

                ANSWER:
                """
            };
        }
        public PromptMessages BuildPromptold(List<SearchResult> chunks, string question)
        {

            //var context = string.Join(
            //    Environment.NewLine + Environment.NewLine,
            //    chunks.Select(c => c.Record.Content));



            var context = string.Join(
                Environment.NewLine + Environment.NewLine,
                chunks.Select(chunk =>
            $"""
            ==================================================
            Source {chunk.Rank}

            Rank:
            {chunk.Rank}

            Similarity:
            {chunk.Similarity:F2}

            Content:
            {chunk.Record.Content}
            ==================================================
            """));

            var systemPrompt = """
            You are a helpful AI assistant.

            Use ONLY the supplied sources.

            Never use outside knowledge.
            """;

                        var userPrompt = $"""
            SOURCES:

            {context}

            QUESTION:

            {question}
            """;

            return new PromptMessages
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt
            };
        }
    }
}
