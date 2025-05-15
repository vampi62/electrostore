using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CameraService;

public interface ICameraService
{
    public Task<IEnumerable<ReadCameraDto>> GetCameras(int limit = 100, int offset = 0, List<int>? idResearch = null);

    public Task<int> GetCamerasCount();

    public Task<ReadCameraDto> GetCameraById(int id);

    public Task<ReadCameraDto> CreateCamera(CreateCameraDto cameraDto);

    public Task<ReadCameraDto> UpdateCamera(int id, UpdateCameraDto cameraDto);

    public Task DeleteCamera(int id);

    public Task<CameraStatusDto> GetCameraStatus(int id_camera);

    public Task<ActionResult> GetCameraCapture(int id_camera);

    public Task<CameraLightDto> SwitchCameraLight(int id_camera, CameraLightDto reqCamera);

    public Task<ActionResult> GetCameraStream(int id_camera, string token);
}