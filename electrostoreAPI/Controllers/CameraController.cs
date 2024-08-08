using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CameraService;

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
        public async Task<ActionResult> GetCameras([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var cameras = await _cameraService.GetCameras(limit, offset);
            return Ok(cameras);
        }

        [HttpGet("{id_camera}")]
        public async Task<ActionResult> GetCameraById([FromRoute] int id_camera)
        {
            var camera = await _cameraService.GetCameraById(id_camera);
            return Ok(camera);
        }

        [HttpGet("{id_camera}/stream")]
        public async Task<ActionResult> GetCameraStream([FromRoute] int id_camera)
        {
            var camera = await _cameraService.GetCameraById(id_camera);
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(camera.url_camera, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return new FileStreamResult(await response.Content.ReadAsStreamAsync(), "video/mp4");
        }

        [HttpPost]
        public async Task<ActionResult> CreateCamera([FromBody] CreateCameraDto camera)
        {
            var newCamera = await _cameraService.CreateCamera(camera);
            return CreatedAtAction(nameof(GetCameraById), new { id_camera = newCamera.id_camera }, newCamera);
        }

        [HttpPut("{id_camera}")]
        public async Task<ActionResult> UpdateCamera([FromRoute] int id_camera, [FromBody] UpdateCameraDto camera)
        {
            var cameraToUpdate = await _cameraService.UpdateCamera(id_camera, camera);
            return Ok(cameraToUpdate);
        }

        [HttpDelete("{id_camera}")]
        public async Task<ActionResult> DeleteCamera([FromRoute] int id_camera)
        {
            await _cameraService.DeleteCamera(id_camera);
            return NoContent();
        }
    }
}