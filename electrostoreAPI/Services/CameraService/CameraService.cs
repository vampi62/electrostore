using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using System.Net.Http.Headers;
using electrostore.Services.SessionService;
using electrostore.Services.JwiService;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CameraService;

public class CameraService : ICameraService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IJwiService _jwiService;
    private readonly IConfiguration _configuration;
    private const string DemoModeKey = "DemoMode";

    public CameraService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IJwiService jwiService, IConfiguration configuration)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _jwiService = jwiService;
        _configuration = configuration;
    }

    // limit the number of camera to 100 and add offset and search parameters
    public async Task<IEnumerable<ReadCameraDto>> GetCameras(int limit = 100, int offset = 0, List<int>? idResearch = null)
    {
        var query = _context.Cameras.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(c => idResearch.Contains(c.id_camera));
        }
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(c => c.id_camera);
        var camera = await query.ToListAsync();
        return _mapper.Map<List<ReadCameraDto>>(camera);
    }

    public async Task<int> GetCamerasCount()
    {
        return await _context.Cameras.CountAsync();
    }

    public async Task<ReadCameraDto> GetCameraById(int id)
    {
        var camera = await _context.Cameras.FindAsync(id) ?? throw new KeyNotFoundException($"Camera with id {id} not found");
        return _mapper.Map<ReadCameraDto>(camera);
    }

    public async Task<ReadCameraDto> CreateCamera(CreateCameraDto cameraDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to create a camera");
        }
        var newCamera = _mapper.Map<Cameras>(cameraDto);
        _context.Cameras.Add(newCamera);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCameraDto>(newCamera);
    }

    public async Task<ReadCameraDto> UpdateCamera(int id, UpdateCameraDto cameraDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a camera");
        }
        var cameraToUpdate = await _context.Cameras.FindAsync(id) ?? throw new KeyNotFoundException($"Camera with id {id} not found");
        if (cameraDto.nom_camera is not null)
        {
            cameraToUpdate.nom_camera = cameraDto.nom_camera;
        }
        if (cameraDto.url_camera is not null)
        {
            cameraToUpdate.url_camera = cameraDto.url_camera;
        }
        if (cameraDto.user_camera is not null)
        {
            cameraToUpdate.user_camera = cameraDto.user_camera;
        }
        if (cameraDto.mdp_camera is not null)
        {
            cameraToUpdate.mdp_camera = cameraDto.mdp_camera;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCameraDto>(cameraToUpdate);
    }

    public async Task DeleteCamera(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete a camera");
        }
        var cameraToDelete = await _context.Cameras.FindAsync(id) ?? throw new KeyNotFoundException($"Camera with id {id} not found");
        _context.Cameras.Remove(cameraToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<CameraStatusDto> GetCameraStatus(int id_camera)
    {
        var camera = await _context.Cameras.FindAsync(id_camera) ?? throw new KeyNotFoundException($"Camera with id {id_camera} not found");
        try
        {
            if (_configuration.GetValue<bool>(DemoModeKey))
            {
                return new CameraStatusDto
                {
                    network = true,
                    statusCode = 200,
                    uptime = 123.45f,
                    espModel = "ESP32",
                    espTemperature = 30.0f,
                    OTAWait = "No updates",
                    OTAUploading = "No uploads",
                    OTAError = "No errors",
                    OTATime = 0,
                    OTARemainingTime = 0,
                    OTAPercentage = 0.0f,
                    ringLightPower = 100,
                    versionScanBox = "1.0.0",
                    cameraResolution = "1920x1080",
                    cameraPID = "12345678",
                    wifiSignalStrength = "-70dBm"
                };
            }
            var client = new HttpClient();
            var urlFluxStream = camera.url_camera.EndsWith('/') ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
            var request = new HttpRequestMessage(HttpMethod.Get, urlFluxStream);
            if (!string.IsNullOrWhiteSpace(camera.user_camera) && !string.IsNullOrWhiteSpace(camera.mdp_camera))
            {
                var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new CameraStatusDto {
                    network = true,
                    statusCode = (int)response.StatusCode
                };
            }
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
            if (json is null)
            {
                return new CameraStatusDto {
                    network = true,
                    statusCode = (int)response.StatusCode
                };
            }
            return new CameraStatusDto {
                network = true,
                statusCode = (int)response.StatusCode,
                uptime = json.TryGetValue("uptime", out var uptime) && uptime.ValueKind == JsonValueKind.Number ? uptime.GetSingle() : 0f,
                espModel = json.TryGetValue("espModel", out var espModel) && espModel.ValueKind == JsonValueKind.String ? espModel.GetString() : string.Empty,
                espTemperature = json.TryGetValue("espTemperature", out var espTemperature) && espTemperature.ValueKind == JsonValueKind.Number ? espTemperature.GetSingle() : 0f,
                OTAWait = json.TryGetValue("OTAWait", out var OTAWait) && OTAWait.ValueKind == JsonValueKind.String ? OTAWait.GetString() : string.Empty,
                OTAUploading = json.TryGetValue("OTAUploading", out var OTAUploading) && OTAUploading.ValueKind == JsonValueKind.String ? OTAUploading.GetString() : string.Empty,
                OTAError = json.TryGetValue("OTAError", out var OTAError) && OTAError.ValueKind == JsonValueKind.String ? OTAError.GetString() : string.Empty,
                OTATime = json.TryGetValue("OTATime", out var OTATime) && OTATime.ValueKind == JsonValueKind.Number ? OTATime.GetInt32() : 0,
                OTARemainingTime = json.TryGetValue("OTARemainingTime", out var OTARemainingTime) && OTARemainingTime.ValueKind == JsonValueKind.Number ? OTARemainingTime.GetInt32() : 0,
                OTAPercentage = json.TryGetValue("OTAPercentage", out var OTAPercentage) && OTAPercentage.ValueKind == JsonValueKind.Number ? OTAPercentage.GetSingle() : 0f,
                ringLightPower = json.TryGetValue("ringLightPower", out var ringLightPower) && ringLightPower.ValueKind == JsonValueKind.Number ? ringLightPower.GetInt32() : 0,
                versionScanBox = json.TryGetValue("versionScanBox", out var versionScanBox) && versionScanBox.ValueKind == JsonValueKind.String ? versionScanBox.GetString() : string.Empty,
                cameraResolution = json.TryGetValue("cameraResolution", out var cameraResolution) && cameraResolution.ValueKind == JsonValueKind.String ? cameraResolution.GetString() : string.Empty,
                cameraPID = json.TryGetValue("cameraPID", out var cameraPID) && cameraPID.ValueKind == JsonValueKind.Number ? cameraPID.GetInt32().ToString() : string.Empty,
                wifiSignalStrength = json.TryGetValue("wifiSignalStrength", out var wifiSignalStrength) && wifiSignalStrength.ValueKind == JsonValueKind.String ? wifiSignalStrength.GetString() : string.Empty
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new CameraStatusDto {
                network = false,
                statusCode = 500
            };
        }
    }

    public async Task<ActionResult> GetCameraCapture(int id_camera)
    {
        var camera = await _context.Cameras.FindAsync(id_camera) ?? throw new KeyNotFoundException($"Camera with id {id_camera} not found");
        try
        {
            if (_configuration.GetValue<bool>(DemoModeKey))
            {
                var demoImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "demo", "demo_camera_capture.jpg");
                if (!System.IO.File.Exists(demoImagePath))
                {
                    throw new FileNotFoundException("Demo image not found", demoImagePath);
                }
                return new FileStreamResult(new FileStream(demoImagePath, FileMode.Open, FileAccess.Read), "image/jpeg");
            }
            var client = new HttpClient();
            var urlFluxStream = camera.url_camera.EndsWith('/') ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
            var request = new HttpRequestMessage(HttpMethod.Get, urlFluxStream + "/capture");
            if (!string.IsNullOrWhiteSpace(camera.user_camera) && !string.IsNullOrWhiteSpace(camera.mdp_camera))
            {
                var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error while getting camera capture: {response.StatusCode}");
            }
            var contentStream = await response.Content.ReadAsStreamAsync();
            return new FileStreamResult(contentStream, "image/jpeg");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception($"Error while getting camera capture: {ex.Message}");
        }
    }

    public async Task<CameraLightDto> SwitchCameraLight(int id_camera, CameraLightDto reqCamera)
    {
        var camera = await _context.Cameras.FindAsync(id_camera) ?? throw new KeyNotFoundException($"Camera with id {id_camera} not found");
        try
        {
            if (_configuration.GetValue<bool>(DemoModeKey))
            {
                return new CameraLightDto { state = reqCamera.state };
            }
            var client = new HttpClient();
            var urlFluxStream = camera.url_camera.EndsWith('/') ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
            var request = new HttpRequestMessage(HttpMethod.Get, urlFluxStream + "/light?state=" + (reqCamera.state ? "on" : "off"));
            if (!string.IsNullOrWhiteSpace(camera.user_camera) && !string.IsNullOrWhiteSpace(camera.mdp_camera))
            {
                var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error while switching camera light: {response.StatusCode}");
            }
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content);
            if (json is null || !json.ContainsKey("ringLightPower"))
            {
                throw new Exception($"Error while switching camera light: {response.StatusCode}");
            }
            return new CameraLightDto { state = json["ringLightPower"].GetBoolean() };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception($"Error while switching camera light: {ex.Message}");
        }
    }

    public async Task<ActionResult> GetCameraStream(int id_camera, string token)
    {
        if (token is null || !_jwiService.ValidateToken(token, "access"))
        {
            throw new UnauthorizedAccessException("Invalid token");
        }
        var camera = await _context.Cameras.FindAsync(id_camera) ?? throw new KeyNotFoundException($"Camera with id {id_camera} not found");
        try
        {
            if (_configuration.GetValue<bool>(DemoModeKey))
            {
                var demoStreamPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "demo", "demo_camera_stream.mjpeg");
                if (!System.IO.File.Exists(demoStreamPath))
                {
                    throw new FileNotFoundException("Demo stream not found", demoStreamPath);
                }
                var demoContentStream = new FileStream(demoStreamPath, FileMode.Open, FileAccess.Read);
                return new FileStreamResult(demoContentStream, "multipart/x-mixed-replace; boundary=--myboundary");
            }
            var client = new HttpClient();
            var urlFluxStream = camera.url_camera.EndsWith('/') ? camera.url_camera.Substring(0, camera.url_camera.Length - 1) : camera.url_camera;
            var request = new HttpRequestMessage(HttpMethod.Get, urlFluxStream + "/stream");
            if (!string.IsNullOrWhiteSpace(camera.user_camera) && !string.IsNullOrWhiteSpace(camera.mdp_camera))
            {
                var byteArray = Encoding.ASCII.GetBytes($"{camera.user_camera}:{camera.mdp_camera}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error while getting camera stream: {response.StatusCode}");
            }
            if (response.Content.Headers.ContentType is null)
            {
                throw new Exception($"Error while getting camera stream: {response.StatusCode}");
            }
            var boundary = (response.Content.Headers.ContentType.Parameters.FirstOrDefault(p => p.Name == "boundary")?.Value) ?? throw new Exception($"Error while getting camera stream: {response.StatusCode}");
            var contentStream = await response.Content.ReadAsStreamAsync();
            return new FileStreamResult(contentStream, "multipart/x-mixed-replace; boundary=" + boundary);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception($"Error while getting camera stream: {ex.Message}");
        }
    }
}