using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CameraService;

public class CameraService : ICameraService
{
    private readonly ApplicationDbContext _context;

    public CameraService(ApplicationDbContext context)
    {
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
        return await query
            .Skip(offset)
            .Take(limit)
            .Select(s => new ReadCameraDto
            {
                id_camera = s.id_camera,
                nom_camera = s.nom_camera,
                url_camera = s.url_camera,
                user_camera = s.user_camera,
                mdp_camera = s.mdp_camera
            }).ToListAsync();
    }

    public async Task<int> GetCamerasCount()
    {
        return await _context.Cameras.CountAsync();
    }

    public async Task<ReadCameraDto> GetCameraById(int id)
    {
        var camera = await _context.Cameras.FindAsync(id) ?? throw new KeyNotFoundException($"Camera with id {id} not found");
        return new ReadCameraDto
        {
            id_camera = camera.id_camera,
            nom_camera = camera.nom_camera,
            url_camera = camera.url_camera,
            user_camera = camera.user_camera,
            mdp_camera = camera.mdp_camera
        };
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
        return new ReadCameraDto
        {
            id_camera = newCamera.id_camera,
            nom_camera = newCamera.nom_camera,
            url_camera = newCamera.url_camera,
            user_camera = newCamera.user_camera,
            mdp_camera = newCamera.mdp_camera
        };
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
        return new ReadCameraDto
        {
            id_camera = cameraToUpdate.id_camera,
            nom_camera = cameraToUpdate.nom_camera,
            url_camera = cameraToUpdate.url_camera,
            user_camera = cameraToUpdate.user_camera,
            mdp_camera = cameraToUpdate.mdp_camera
        };
    }

    public async Task DeleteCamera(int id)
    {
        var cameraToDelete = await _context.Cameras.FindAsync(id) ?? throw new KeyNotFoundException($"Camera with id {id} not found");
        _context.Cameras.Remove(cameraToDelete);
        await _context.SaveChangesAsync();
    }
}