using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Services.Tools
{
    public class ToolExecutor : IToolExecutor
    {
        private readonly IEnumerable<ITool> _tools;
        public ToolExecutor(IEnumerable<ITool> tools)
        {
            _tools = tools;
        }
        public async Task<ToolResult> ExecuteAsync(ToolCallRequest request)
        {
            var tool = _tools.FirstOrDefault(
                t => t.Definition.Name.Equals(
                    request.ToolName,
                    StringComparison.OrdinalIgnoreCase));

            if (tool == null)
            {
                return new ToolResult 
                { 
                    ToolName = request.ToolName, 
                    Success = false, 
                    Error = $"Tool '{request.ToolName}' was not found." 
                };
            }

            try
            {
                var result = await tool.ExecuteAsync(request);
                return new ToolResult
                {
                    ToolName = request.ToolName,
                    Success = result.Success,
                    Data = result.Data,
                    Error = result.Error,
                    Metadata = result.Metadata
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    ToolName = request.ToolName,
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}
