using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Tools;
using System.Data;
using System.Globalization;

namespace AIChatAssistant.Services
{
    public class CalculatorService : ICalculatorService
    {
        public Task<ToolResult> EvaluateAsync(string expression)
        {
            try
            {
                var table = new DataTable();

                var result = table.Compute(expression, null);

                return Task.FromResult(new ToolResult
                {
                    Success = true,
                    Data = Convert.ToString(result, CultureInfo.InvariantCulture) ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ToolResult
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }
    }
}
