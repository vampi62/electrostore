using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.UserPushSubscriptionService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ElectrostoreAPI.Controllers
{
    [ApiController]
    [Route("api/user/{id_user}/push-subscriptions")]

    public class UserPushSubscriptionController : ControllerBase
    {
        private readonly IUserPushSubscriptionService _userPushSubscriptionService;

        public UserPushSubscriptionController(IUserPushSubscriptionService userPushSubscriptionService)
        {
            _userPushSubscriptionService = userPushSubscriptionService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<PaginatedResponseDto<ReadUserPushSubscriptionDto>>> GetPushSubscriptions(
            [FromRoute] int id_user,
            [FromQuery] int limit = 100,
            [FromQuery] int offset = 0)
        {
            var result = await _userPushSubscriptionService.GetPushSubscriptionsByUserId(id_user, limit, offset);
            return Ok(result);
        }

        [HttpGet("{id_subscription}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadUserPushSubscriptionDto>> GetPushSubscriptionById(
            [FromRoute] int id_user,
            [FromRoute] int id_subscription)
        {
            var result = await _userPushSubscriptionService.GetPushSubscriptionById(id_subscription, id_user);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadUserPushSubscriptionDto>> CreatePushSubscription(
            [FromRoute] int id_user,
            [FromBody] CreateUserPushSubscriptionDtoByUserId dto)
        {
            var dtoFull = new CreateUserPushSubscriptionDto
            {
                endpoint = dto.endpoint,
                p256dh = dto.p256dh,
                auth = dto.auth,
                device_name = dto.device_name,
                id_user = id_user
            };
            var result = await _userPushSubscriptionService.CreatePushSubscription(dtoFull);
            return CreatedAtAction(nameof(GetPushSubscriptionById), new { id_user, id_subscription = result.id_push_subscription }, result);
        }

        [HttpDelete("{id_subscription}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeletePushSubscription(
            [FromRoute] int id_user,
            [FromRoute] int id_subscription)
        {
            await _userPushSubscriptionService.DeletePushSubscription(id_subscription, id_user);
            return NoContent();
        }
    }
}
