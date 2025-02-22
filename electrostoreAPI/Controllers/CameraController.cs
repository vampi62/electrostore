using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using electrostore.Dto;
using electrostore.Services.CameraService;
using Swashbuckle.AspNetCore.Annotations;
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
        public async Task<ActionResult<IEnumerable<ReadCameraDto>>> GetCameras([FromQuery] int limit = 100, [FromQuery] int offset = 0, [FromQuery, SwaggerParameter(Description = "(Optional) Fields to select list of ID to research in the base. Multiple values can be specified by separating them with ','.")] string? idResearch = null)
        {
            var idList = string.IsNullOrWhiteSpace(idResearch) ? null : idResearch.Split(',').Where(id => int.TryParse(id, out _)).Select(int.Parse).ToList();
            var cameras = await _cameraService.GetCameras(limit, offset, idList);
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
            if (token is null || !_jwiService.ValidateToken(token, "access"))
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
                    return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to connect to the camera." } }));
                }
                using (var httpClient = new HttpClient())
                {
                    if (!string.IsNullOrWhiteSpace(camera.user_camera) || !string.IsNullOrWhiteSpace(camera.mdp_camera))
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    // si camera.url_camera termine par un /, on le supprime
                    var urlFluxStream = camera.url_camera.EndsWith("/") ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
                    var response = await httpClient.GetAsync(urlFluxStream + "/stream", HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to retrieve video stream." } }));
                    }
                    if (response.Content.Headers.ContentType is null)
                    {
                        return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to retrieve video stream." } }));
                    }
                    var boundary = response.Content.Headers.ContentType.Parameters.FirstOrDefault(p => p.Name == "boundary")?.Value;
                    if (boundary is null)
                    {
                        return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to retrieve video stream." } }));
                    }
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    return new FileStreamResult(contentStream, "multipart/x-mixed-replace; boundary=" + boundary);
                }
            }
            catch
            {
                return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to connect to the camera." } }));
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
                    return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to connect to the camera." } }));
                }
                using (var httpClient = new HttpClient())
                {
                    if (!string.IsNullOrWhiteSpace(camera.user_camera) && !string.IsNullOrWhiteSpace(camera.mdp_camera))
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    var urlLight = camera.url_camera.EndsWith("/") ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
                    var response = await httpClient.GetAsync(urlLight + "/light?state=" + (reqCamera.state ? "on" : "off"));
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to switch camera light state." } }));
                    }
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    if (json is null || json.ContainsKey("ringLightPower") == false)
                    {
                        return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to switch camera light state." } }));
                    }
                    var newLightDto = new CameraLightDto { state = (bool)json["ringLightPower"] };
                    return Ok(newLightDto);
                }
            }
            catch
            {
                return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to connect to the camera." } }));
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
                    return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to connect to the camera." } }));
                }
                using (var httpClient = new HttpClient())
                {
                    if (!string.IsNullOrWhiteSpace(camera.user_camera) && !string.IsNullOrWhiteSpace(camera.mdp_camera))
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    // si camera.url_camera termine par un /, on le supprime
                    var urlFluxStream = camera.url_camera.EndsWith("/") ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
                    var response = await httpClient.GetAsync(urlFluxStream + "/capture", HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        return StatusCode((int)response.StatusCode, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to retrieve camera capture." } }));
                    }
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    return new FileStreamResult(contentStream, "image/jpeg");
                }
            }
            catch
            {
                return StatusCode(500, JsonSerializer.Serialize(new Dictionary<string, string> { { "errors", "Unable to connect to the camera." } }));
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
                var newCameraStatusDto = new CameraStatusDto();
                if (pingReply.Status != System.Net.NetworkInformation.IPStatus.Success)
                {
                    newCameraStatusDto = new CameraStatusDto {
                        network = false,
                        statusCode = 404
                    };
                    return Ok(newCameraStatusDto);
                }
                using (var httpClient = new HttpClient())
                {
                    if (!string.IsNullOrWhiteSpace(camera.user_camera) && !string.IsNullOrWhiteSpace(camera.mdp_camera))
                    {
                        var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    // si camera.url_camera termine par un /, on le supprime
                    var urlFluxStream = camera.url_camera.EndsWith("/") ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
                    var response = await httpClient.GetAsync(urlFluxStream + "/status", HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        newCameraStatusDto = new CameraStatusDto {
                            network = true,
                            statusCode = (int)response.StatusCode
                        };
                        return Ok(newCameraStatusDto);
                    }
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
                    if (json is null)
                    {
                        newCameraStatusDto = new CameraStatusDto {
                            network = true,
                            statusCode = (int)response.StatusCode
                        };
                        return Ok(newCameraStatusDto);
                    }
                    newCameraStatusDto = new CameraStatusDto {
                        network = true,
                        statusCode = (int)response.StatusCode,
                        uptime = json.TryGetValue("uptime", out var uptime) && uptime.ValueKind == JsonValueKind.Number ? uptime.GetSingle() : 0f,
                        espModel = json.TryGetValue("espModel", out var espModel) && espModel.ValueKind == JsonValueKind.String ? espModel.GetString() : string.Empty,
                        espTemperature = json.TryGetValue("espTemperature", out var espTemperature) && espTemperature.ValueKind == JsonValueKind.Number ? espTemperature.GetSingle() : 0f,
                        ringLightPower = json.TryGetValue("ringLightPower", out var ringLightPower) && ringLightPower.ValueKind == JsonValueKind.Number ? ringLightPower.GetInt32() : 0,
                        versionScanBox = json.TryGetValue("versionScanBox", out var versionScanBox) && versionScanBox.ValueKind == JsonValueKind.String ? versionScanBox.GetString() : string.Empty,
                        cameraResolution = json.TryGetValue("cameraResolution", out var cameraResolution) && cameraResolution.ValueKind == JsonValueKind.String ? cameraResolution.GetString() : string.Empty,
                        cameraPID = json.TryGetValue("cameraPID", out var cameraPID) && cameraPID.ValueKind == JsonValueKind.Number ? cameraPID.GetInt32().ToString() : string.Empty,
                        wifiSignalStrength = json.TryGetValue("wifiSignalStrength", out var wifiSignalStrength) && wifiSignalStrength.ValueKind == JsonValueKind.String ? wifiSignalStrength.GetString() : string.Empty
                    };
                    return Ok(newCameraStatusDto);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                var newCameraStatusDto = new CameraStatusDto {
                    network = false,
                    statusCode = 500
                };
                return Ok(newCameraStatusDto);
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