using Microsoft.AspNetCore.Mvc;
using electrostore.Dto;
using electrostore.Services.CameraService;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using electrostore.Services.JwtService;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/camera")]
    public class CameraController : ControllerBase
    {
        private readonly ICameraService _cameraService;
        private readonly JwtService _jwtService;

        public CameraController(ICameraService cameraService, JwtService jwtService)
        {
            _cameraService = cameraService;
            _jwtService = jwtService;
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
            if (camera.Result is BadRequestObjectResult)
            {
                return camera.Result;
            }
            if (camera.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(camera.Value);
        }

        [HttpGet("{id_camera}/stream")]
        [AllowAnonymous]
        public async Task<ActionResult> GetCameraStream([FromRoute] int id_camera, [FromQuery] string token)
        {
            if (token == null || !_jwtService.ValidateToken(token))
            {
                return Unauthorized();
            }
            var camera = await _cameraService.GetCameraById(id_camera);
            if (camera.Result is BadRequestObjectResult)
            {
                return camera.Result;
            }
            if (camera.Value == null)
            {
                return StatusCode(500);
            }
            try
            {
                // ping the camera to check if it is reachable
                var ping = new System.Net.NetworkInformation.Ping();
                var DomainOrIP = new System.Text.RegularExpressions.Regex(@"(?<protocol>http[s]?:\/\/)?(?<domain>[a-zA-Z0-9\.\-]+)(?<port>:[0-9]+)?(?<uri>.*)").Match(camera.Value.url_camera).Groups["domain"].Value;
                var pingReply = ping.Send(DomainOrIP, 2000);
                if (pingReply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    return StatusCode(504, "Impossible de se connecter à la caméra.");
                }
                using (var httpClient = new HttpClient())
                {
                    if (camera.Value.user_camera != null && camera.Value.mdp_camera != null)
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.Value.user_camera}:{camera.Value.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    // si camera.Value.url_camera termine par un /, on le supprime
                    var urlFluxStream = camera.Value.url_camera.EndsWith("/") ? camera.Value.url_camera.Substring(0, camera.Value.url_camera.Length - 1) : camera.Value.url_camera;
                    var response = await httpClient.GetAsync(urlFluxStream + "/stream", HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, "Erreur lors de la récupération du flux vidéo.");
                    }
                    if (response.Content.Headers.ContentType == null)
                    {
                        return StatusCode(500, "Impossible de récupérer le flux vidéo.");
                    }
                    var boundary = response.Content.Headers.ContentType.Parameters.FirstOrDefault(p => p.Name == "boundary")?.Value;
                    if (boundary == null)
                    {
                        return StatusCode(500, "Impossible de récupérer le flux vidéo.");
                    }
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    return new FileStreamResult(contentStream, "multipart/x-mixed-replace; boundary=" + boundary);
                }
            }
            catch
            {
                return StatusCode(500, "Impossible de se connecter à la caméra.");
            }
        }

        [HttpPost("{id_camera}/light")]
        public async Task<ActionResult> SwitchCameraLight([FromRoute] int id_camera, [FromBody] bool state)
        {
            var camera = await _cameraService.GetCameraById(id_camera);
            if (camera.Result is BadRequestObjectResult)
            {
                return camera.Result;
            }
            if (camera.Value == null)
            {
                return StatusCode(500);
            }
            try
            {
                using (var httpClient = new HttpClient())
                {
                    if (camera.Value.user_camera != null && camera.Value.mdp_camera != null)
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.Value.user_camera}:{camera.Value.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    var urlLight = camera.Value.url_camera.EndsWith("/") ? camera.Value.url_camera.Substring(0, camera.Value.url_camera.Length - 1) : camera.Value.url_camera;
                    var response = await httpClient.GetAsync(urlLight + "/light?state=" + (state ? "on" : "off"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, "Erreur lors de la modification de l'état de la lumière.");
                    }
                    return Ok();
                }
            }
            catch
            {
                return StatusCode(500, "Impossible de se connecter à la caméra.");
            }
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
            if (cameraToUpdate.Result is BadRequestObjectResult)
            {
                return cameraToUpdate.Result;
            }
            if (cameraToUpdate.Value == null)
            {
                return StatusCode(500);
            }
            return Ok(cameraToUpdate.Value);
        }

        [HttpDelete("{id_camera}")]
        public async Task<ActionResult> DeleteCamera([FromRoute] int id_camera)
        {
            await _cameraService.DeleteCamera(id_camera);
            return NoContent();
        }
    }
}