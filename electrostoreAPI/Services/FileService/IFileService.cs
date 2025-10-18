using electrostore.Dto;

namespace electrostore.Services.FileService;

public interface IFileService
{
    Task<GetFileResult> GetFile(string url);
    Task<SaveFileResult> SaveFile(string basePath, IFormFile file);
    Task<SaveFileResult> GenerateThumbnail(string sourcePath, string destPath, int width, int height);
    Task DeleteFile(string url);
    Task CreateDirectory(string path);
    Task DeleteDirectory(string path);
}