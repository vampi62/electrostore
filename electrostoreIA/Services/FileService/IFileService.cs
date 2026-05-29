using ElectrostoreIA.Dto;

namespace ElectrostoreIA.Services.FileService;

public interface IFileService
{
    Task<GetFileResult> GetFile(string path);
    Task<bool> FileExists(string path);
    Task<SaveFileResult> SaveFile(string basePath, string fullFileName, string contentType, Stream data, bool overwrite = false);
    Task DeleteFile(string path);
    Task CreateDirectory(string path);
    Task DeleteDirectory(string path);
}