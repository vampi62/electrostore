using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CameraService;

public interface ICameraService
{
    public Task<IEnumerable<ReadCameraDto>> GetCameras(int limit = 100, int offset = 0);

    public Task<ActionResult<ReadCameraDto>> GetCameraById(int id);

    public Task<ReadCameraDto> CreateCamera(CreateCameraDto cameraDto);

    public Task<ActionResult<ReadCameraDto>> UpdateCamera(int id, UpdateCameraDto cameraDto);

    public Task<IActionResult> DeleteCamera(int id);
}