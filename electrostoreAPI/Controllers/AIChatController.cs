using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.AiChatService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/ai/chat")]

    public class AIChatController : ControllerBase
    {
        private readonly IAiChatService _aiChatService;

        public AIChatController(IAiChatService aiChatService)
        {
            _aiChatService = aiChatService;
        }

        [HttpPost("messages")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<SendAiChatMessageResponseDto>> SendMessage([FromForm] CreateAiChatMessageDto message,
        [FromQuery, SwaggerParameter(Description = "If true, the final assistant answer is streamed back as text/event-stream instead of a one-shot JSON response.")] bool stream = false,
        CancellationToken cancellationToken = default)
        {
            if (stream)
            {
                await _aiChatService.StreamMessage(message, Response, cancellationToken);
                return new EmptyResult();
            }
            var response = await _aiChatService.SendMessage(message, cancellationToken);
            return Ok(response);
        }
    }
}
