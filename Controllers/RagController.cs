using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AIChatAssistant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RagController : ControllerBase
    {
        private readonly IRagService _ragService;
        public RagController(IRagService ragService)
        {
            _ragService = ragService;
        }

        [HttpPost("chat")]
        public async Task<ActionResult<ChatResponse>> Chat(ChatRequest request)
        {
            var response = await _ragService.AskAsync(request);

            return Ok(response);
        }
    }
}
