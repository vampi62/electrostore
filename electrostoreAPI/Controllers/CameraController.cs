using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CameraService;
using Swashbuckle.AspNetCore.Annotations;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/camera")]
    public class CameraController : ControllerBase
    {
        private readonly ICameraService _cameraService;

        public CameraController(ICameraService cameraService)
        {
            _cameraService = cameraService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadCameraDto>>> GetCameras([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] List<int>? idResearch = null)
        {
            var cameras = await _cameraService.GetCameras(limit, offset, idResearch);
            var CountList = await _cameraService.GetCamerasCount();
            Response.Headers.Add("X-Total-Count", CountList.ToString());
            Response.Headers.Add("Access-Control-Expose-Headers", "X-Total-Count");
            return Ok(cameras);
        }

        [HttpGet("{id_camera}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCameraDto>> GetCameraById([FromRoute] int id_camera)
        {
            var camera = await _cameraService.GetCameraById(id_camera);
            return Ok(camera);
        }

        [HttpGet("{id_camera}/stream")]
        [AllowAnonymous]
        public async Task<ActionResult> GetCameraStream([FromRoute] int id_camera, [FromQuery] string token)
        {
            return await _cameraService.GetCameraStream(id_camera, token);
        }

        [HttpPost("{id_camera}/light")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<CameraLightDto>> SwitchCameraLight([FromRoute] int id_camera, [FromBody] CameraLightDto reqCamera)
        {
            var newLightDto = await _cameraService.SwitchCameraLight(id_camera, reqCamera);
            return Ok(newLightDto);
        }

        [HttpGet("{id_camera}/capture")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetCameraCapture([FromRoute] int id_camera)
        {
            return await _cameraService.GetCameraCapture(id_camera);
        }

        [HttpGet("{id_camera}/status")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<CameraStatusDto>> GetCameraStatus([FromRoute] int id_camera)
        {
            var status = await _cameraService.GetCameraStatus(id_camera);
            return Ok(status);
        }

        [HttpPost]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCameraDto>> CreateCamera([FromBody] CreateCameraDto camera)
        {
            var newCamera = await _cameraService.CreateCamera(camera);
            return CreatedAtAction(nameof(GetCameraById), new { id_camera = newCamera.id_camera }, newCamera);
        }

        [HttpPut("{id_camera}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<ReadCameraDto>> UpdateCamera([FromRoute] int id_camera, [FromBody] UpdateCameraDto camera)
        {
            var cameraToUpdate = await _cameraService.UpdateCamera(id_camera, camera);
            return Ok(cameraToUpdate);
        }

        [HttpDelete("{id_camera}")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> DeleteCamera([FromRoute] int id_camera)
        {
            await _cameraService.DeleteCamera(id_camera);
            return NoContent();
        }
    }
}