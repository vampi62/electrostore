using electrostore.Dto;

namespace electrostore.Services.FileService;

public class FileService
{
    public async Task<GetFileResult> GetFile(string basePath, string url)
    {
        var pathImg = Path.Combine(basePath, url);
        if (!File.Exists(pathImg))
        {
            return new GetFileResult
            {
                Success = false,
                ErrorMessage = "File not found",
                FilePath = "",
                MimeType = ""
            };
        } else {
            var ext = Path.GetExtension(pathImg);
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
            return await Task.FromResult(new GetFileResult
            {
                Success = true,
                FilePath = pathImg,
                MimeType = mimeType
            });
        }
    }
}