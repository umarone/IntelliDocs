using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Services.Tools
{
    public class CalculatorTool : ITool
    {
        public ToolDefinition Definition => new()
        {
            Name = "Calculator",

            Description = "Performs mathematical calculations.",

            Parameters =
            [
                "expression"
            ],

            WhenToUse =
            [
                "Use this tool when the user wants the numerical result of a calculation.",
                "Use this tool when the user asks to evaluate a mathematical expression.",
                "Use this tool when the user asks for the numerical result of a percentage calculation.",
                "Do NOT use this tool when the user asks how to perform or understand a calculation.",
                "Do NOT use this tool when the user asks for an explanation or step-by-step method and the required values are already known."
            ],
            Examples =
            [
                "What is 2 + 2?",
                "Calculate 25 * 8.",
                "What is 15% of 200?",
                "How do I calculate 25% of 400? -> Do not use this tool; explain the calculation directly."
            ]
        };

        private readonly ICalculatorService _calculatorService;
        public CalculatorTool(ICalculatorService calculatorService)
        {
            _calculatorService = calculatorService;
        }
        public async Task<ToolResult> ExecuteAsync(ToolCallRequest request)
        {
            if (!request.Arguments.TryGetValue(
                    "expression",
                    out var expressionValue))
            {
                return new ToolResult
                {
                    ToolName = Definition.Name,
                    Success = false,
                    Error = "Expression is required."
                };
            }

            var expression =
                expressionValue?.ToString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(expression))
            {
                return new ToolResult
                {
                    ToolName = Definition.Name,
                    Success = false,
                    Error = "Expression cannot be empty."
                };
            }

            var result =
                await _calculatorService.EvaluateAsync(expression);

            return new ToolResult
            {
                ToolName = Definition.Name,
                Success = result.Success,
                Data = result.Data,
                Error = result.Error,
                Metadata = result.Metadata
            };
        }
    }
}
