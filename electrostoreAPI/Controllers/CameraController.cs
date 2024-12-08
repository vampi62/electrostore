using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CameraService;
using System.Net.Http.Headers;
using System.Text;
using electrostore.Services.JwiService;
using System.Text.Json;

namespace electrostore.Controllers
{
    [ApiController]
    [Route("api/camera")]
    public class CameraController : ControllerBase
    {
        private readonly ICameraService _cameraService;
        private readonly IJwiService _jwiService;

        public CameraController(ICameraService cameraService, IJwiService jwiService)
        {
            _cameraService = cameraService;
            _jwiService = jwiService;
        }

        [HttpGet]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<IEnumerable<ReadCameraDto>>> GetCameras([FromQuery] int limit = 100, [FromQuery] int offset = 0)
        {
            var cameras = await _cameraService.GetCameras(limit, offset);
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
            if (token == null || !_jwiService.ValidateToken(token, "access"))
            {
                return Unauthorized();
            }
            var camera = await _cameraService.GetCameraById(id_camera);
            try
            {
                // ping the camera to check if it is reachable
                var ping = new System.Net.NetworkInformation.Ping();
                var DomainOrIP = new System.Text.RegularExpressions.Regex(@"(?<protocol>http[s]?:\/\/)?(?<domain>[a-zA-Z0-9\.\-]+)(?<port>:[0-9]+)?(?<uri>.*)").Match(camera.url_camera).Groups["domain"].Value;
                var pingReply = ping.Send(DomainOrIP, 2000);
                if (pingReply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    return StatusCode(504, "Unable to connect to the camera.");
                }
                using (var httpClient = new HttpClient())
                {
                    if (camera.user_camera != null && camera.mdp_camera != null)
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    // si camera.url_camera termine par un /, on le supprime
                    var urlFluxStream = camera.url_camera.EndsWith("/") ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
                    var response = await httpClient.GetAsync(urlFluxStream + "/stream", HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, "Unable to retrieve video stream.");
                    }
                    if (response.Content.Headers.ContentType == null)
                    {
                        return StatusCode(500, "Unable to retrieve video stream.");
                    }
                    var boundary = response.Content.Headers.ContentType.Parameters.FirstOrDefault(p => p.Name == "boundary")?.Value;
                    if (boundary == null)
                    {
                        return StatusCode(500, "Unable to retrieve video stream.");
                    }
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    return new FileStreamResult(contentStream, "multipart/x-mixed-replace; boundary=" + boundary);
                }
            }
            catch
            {
                return StatusCode(500, "Unable to connect to the camera.");
            }
        }

        [HttpPost("{id_camera}/light")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<CameraLightDto>> SwitchCameraLight([FromRoute] int id_camera, [FromBody] CameraLightDto reqCamera)
        {
            var camera = await _cameraService.GetCameraById(id_camera);
            try
            {
                // ping the camera to check if it is reachable
                var ping = new System.Net.NetworkInformation.Ping();
                var DomainOrIP = new System.Text.RegularExpressions.Regex(@"(?<protocol>http[s]?:\/\/)?(?<domain>[a-zA-Z0-9\.\-]+)(?<port>:[0-9]+)?(?<uri>.*)").Match(camera.url_camera).Groups["domain"].Value;
                var pingReply = ping.Send(DomainOrIP, 2000);
                if (pingReply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    return StatusCode(504, "Unable to connect to the camera.");
                }
                using (var httpClient = new HttpClient())
                {
                    if (camera.user_camera != null && camera.mdp_camera != null)
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    var urlLight = camera.url_camera.EndsWith("/") ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
                    var response = await httpClient.GetAsync(urlLight + "/light?state=" + (reqCamera.state ? "on" : "off"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, "Unable to switch camera light state.");
                    }
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    if (json == null || json.ContainsKey("ringLightPower") == false)
                    {
                        return StatusCode(500, "Unable to switch camera light state.");
                    }
                    var newLightDto = new CameraLightDto { state = (bool)json["ringLightPower"] };
                    return Ok(newLightDto);
                }
            }
            catch
            {
                return StatusCode(500, "Unable to connect to the camera.");
            }
        }

        [HttpGet("{id_camera}/capture")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult> GetCameraCapture([FromRoute] int id_camera)
        {
            var camera = await _cameraService.GetCameraById(id_camera);
            try
            {
                // ping the camera to check if it is reachable
                var ping = new System.Net.NetworkInformation.Ping();
                var DomainOrIP = new System.Text.RegularExpressions.Regex(@"(?<protocol>http[s]?:\/\/)?(?<domain>[a-zA-Z0-9\.\-]+)(?<port>:[0-9]+)?(?<uri>.*)").Match(camera.url_camera).Groups["domain"].Value;
                var pingReply = ping.Send(DomainOrIP, 2000);
                if (pingReply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    return StatusCode(504, "Unable to connect to the camera.");
                }
                using (var httpClient = new HttpClient())
                {
                    if (camera.user_camera != null && camera.mdp_camera != null)
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    // si camera.url_camera termine par un /, on le supprime
                    var urlFluxStream = camera.url_camera.EndsWith("/") ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
                    var response = await httpClient.GetAsync(urlFluxStream + "/capture", HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, "Unable to retrieve camera capture.");
                    }
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    return new FileStreamResult(contentStream, "image/jpeg");
                }
            }
            catch
            {
                return StatusCode(500, "Unable to connect to the camera.");
            }
        }

        [HttpGet("{id_camera}/status")]
        [Authorize(Policy = "AccessToken")]
        public async Task<ActionResult<CameraStatusDto>> GetCameraStatus([FromRoute] int id_camera)
        {
            var camera = await _cameraService.GetCameraById(id_camera);
            try
            {
                // ping the camera to check if it is reachable
                var ping = new System.Net.NetworkInformation.Ping();
                var DomainOrIP = new System.Text.RegularExpressions.Regex(@"(?<protocol>http[s]?:\/\/)?(?<domain>[a-zA-Z0-9\.\-]+)(?<port>:[0-9]+)?(?<uri>.*)").Match(camera.url_camera).Groups["domain"].Value;
                var pingReply = ping.Send(DomainOrIP, 2000);
                if (pingReply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    return StatusCode(504, "Unable to connect to the camera.");
                }
                using (var httpClient = new HttpClient())
                {
                    if (camera.user_camera != null && camera.mdp_camera != null)
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    // si camera.url_camera termine par un /, on le supprime
                    var urlFluxStream = camera.url_camera.EndsWith("/") ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
                    var response = await httpClient.GetAsync(urlFluxStream + "/status", HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, "Unable to retrieve camera status.");
                    }
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    if (json == null)
                    {
                        return StatusCode(500, "Unable to retrieve camera status.");
                    }
                    var newCameraStatusDto = new CameraStatusDto {
                        uptime = (float)json["uptime"],
                        espModel = (string)json["espModel"],
                        espTemperature = (float)json["espTemperature"],
                        ringLightPower = (int)json["ringLightPower"],
                        versionScanBox = (string)json["versionScanBox"],
                        cameraResolution = (string)json["cameraResolution"],
                        cameraPID = (string)json["cameraPID"],
                        wifiSignalStrength = (string)json["wifiSignalStrength"]
                    };
                    return Ok(newCameraStatusDto);
                }
            }
            catch
            {
                return StatusCode(500, "Unable to connect to the camera.");
            }
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