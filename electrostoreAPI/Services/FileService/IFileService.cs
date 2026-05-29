using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.FileService;

public interface IFileService
{
    Task<GetFileResult> GetFile(string path);
    Task<bool> FileExists(string path);
    Task<SaveFileResult> SaveFile(string basePath, string fullFileName, string contentType, Stream data, bool overwrite = false);
    Task<SaveFileResult> GenerateThumbnail(string sourcePath, string destPath, int width, int height);
    Task DeleteFile(string path);
    Task CreateDirectory(string path);
    Task DeleteDirectory(string path);
}