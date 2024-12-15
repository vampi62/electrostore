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
}