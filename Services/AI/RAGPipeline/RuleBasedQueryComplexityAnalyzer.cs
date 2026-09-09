using AIChatAssistant.Common;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.RAG;

namespace AIChatAssistant.Services.AI.RAGPipeline
{
    public class RuleBasedQueryComplexityAnalyzer : IQueryComplexityAnalyzer
    {
        public Task<QueryComplexityResult> AnalyzeAsync(
            string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Task.FromResult(
                    new QueryComplexityResult
                    {
                        Complexity = QueryComplexity.Normal,
                        Score = 0.5,
                        Reason = "Question was empty or invalid."
                    });
            }

            var words =
                question
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

            var wordCount = words.Length;

            var clauseCount =
                question.Count(
                    character => character == ',' ||
                                 character == ';');

            var questionMarks =
                question.Count(
                    character => character == '?');

            var score = 0.0;

            // ------------------------------------------
            // Query length
            // ------------------------------------------

            if (wordCount <= 6)
            {
                score += 0.2;
            }
            else if (wordCount <= 15)
            {
                score += 0.4;
            }
            else
            {
                score += 0.6;
            }

            // ------------------------------------------
            // Multiple clauses
            // ------------------------------------------

            if (clauseCount >= 1)
            {
                score += 0.15;
            }

            if (clauseCount >= 2)
            {
                score += 0.15;
            }

            // ------------------------------------------
            // Multiple questions
            // ------------------------------------------

            if (questionMarks > 1)
            {
                score += 0.2;
            }

            score = Math.Clamp(score, 0.0, 1.0);

            var complexity =
                score < 0.35
                    ? QueryComplexity.Simple
                    : score < 0.65
                        ? QueryComplexity.Normal
                        : QueryComplexity.Complex;

            return Task.FromResult(
                new QueryComplexityResult
                {
                    Complexity = complexity,
                    Score = score,
                    Reason =
                        $"WordCount={wordCount}, " +
                        $"Clauses={clauseCount}, " +
                        $"QuestionMarks={questionMarks}"
                });
        }
    }
}
