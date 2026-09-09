using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Tools;
using AIChatAssistant.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIChatAssistant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatProviderFactory _providerFactory;
        private readonly IToolExecutor _toolExecutor;
        public ChatController(IChatProviderFactory providerFactory, IToolExecutor toolExecutor)
        {
            _providerFactory = providerFactory;
            _toolExecutor = toolExecutor;
        }
        [HttpPost("SendChatRequest")]
        public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request)
        {
            var provider = _providerFactory.GetProvider();
            var response = await provider.ChatAsync(request);
            return Ok(response);
        }
        [HttpPost("StreamChat")]
        public async Task StreamChat(ChatRequest request)
        {
            var provider = _providerFactory.GetProvider();
            Response.ContentType = "text/event-stream";
            await foreach (var chunk in provider.StreamAsync(request))
            {
                Console.WriteLine("the chunk is ::" + chunk.Content);
                //await Response.WriteAsync(chunk.Content);
                await Response.WriteAsync($"data: {chunk.Content}\n\n");
                await Response.Body.FlushAsync();
            }
        }
        [HttpGet("TestCalculator")]
        public async Task<IActionResult> TestCalculator()
        {
            var request = new ToolCallRequest
            {
                ToolName = "Calculator",
                Arguments = new Dictionary<string, object>
                {
                    { "expression", "2+2" }
                }
            };
            var result = await _toolExecutor.ExecuteAsync(request);
            return Ok(result);
        }
    }
}
