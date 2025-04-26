using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CameraService;

public class CameraService : ICameraService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public CameraService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    // limit the number of camera to 100 and add offset and search parameters
    public async Task<IEnumerable<ReadCameraDto>> GetCameras(int limit = 100, int offset = 0, List<int>? idResearch = null)
    {
        var query = _context.Cameras.AsQueryable();
        if (idResearch != null)
        {
            query = query.Where(b => idResearch.Contains(b.id_camera));
        }
        query = query.Skip(offset).Take(limit);
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
        var newCamera = new Cameras
        {
            nom_camera = cameraDto.nom_camera,
            url_camera = cameraDto.url_camera,
            user_camera = cameraDto.user_camera,
            mdp_camera = cameraDto.mdp_camera
        };
        _context.Cameras.Add(newCamera);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCameraDto>(newCamera);
    }

    public async Task<ReadCameraDto> UpdateCamera(int id, UpdateCameraDto cameraDto)
    {
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
        var cameraToDelete = await _context.Cameras.FindAsync(id) ?? throw new KeyNotFoundException($"Camera with id {id} not found");
        _context.Cameras.Remove(cameraToDelete);
        await _context.SaveChangesAsync();
    }
}