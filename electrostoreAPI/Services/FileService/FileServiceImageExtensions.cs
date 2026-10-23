using ElectrostoreAPI.Dto;
using SkiaSharp;

namespace ElectrostoreAPI.Services.FileService;

public static class FileServiceImageExtensions
{
    private const int ThumbnailSize = 256;

    public static async Task<SaveFileResult> GenerateThumbnail(this IFileService fileService, string sourcePath, string destPath, int width, int height)
    {
        // read the source image, generate the thumbnail and save it (works with any storage backend)
        var file = await fileService.GetFile(sourcePath);
        if (!file.success || file.file_stream == null)
        {
            return new SaveFileResult
            {
                path = "",
                mime_type = ""
            };
        }
        using var original = SKBitmap.Decode(file.file_stream);
        if (original == null)
        {
            return new SaveFileResult
            {
                path = "",
                mime_type = ""
            };
        }
        // fit inside width x height while keeping the aspect ratio
        var scale = Math.Min((float)width / original.Width, (float)height / original.Height);
        var info = new SKImageInfo(
            Math.Max(1, (int)(original.Width * scale)),
            Math.Max(1, (int)(original.Height * scale)));
        using var resized = original.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell));
        using var data = resized.Encode(SKEncodedImageFormat.Jpeg, 85);
        var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Position = 0;
        return await fileService.SaveFile(destPath, Path.GetFileName(sourcePath), "image/jpeg", ms);
    }

    // Deletes the previous picture and thumbnail (if any). Returns true when at least one file was deleted.
    public static async Task<bool> DeleteImageWithThumbnail(this IFileService fileService, string? picturePath, string? thumbnailPath)
    {
        var deleted = false;
        if (picturePath is not null)
        {
            await fileService.DeleteFile(picturePath);
            deleted = true;
        }
        if (thumbnailPath is not null)
        {
            await fileService.DeleteFile(thumbnailPath);
            deleted = true;
        }
        return deleted;
    }

    // Saves the uploaded image in imagesPath and generates its thumbnail in thumbnailsPath.
    public static async Task<(string picturePath, string thumbnailPath)> SaveImageWithThumbnail(this IFileService fileService, string imagesPath, string thumbnailsPath, IFormFile file)
    {
        var savedImg = await fileService.SaveFile(imagesPath, file.FileName, file.ContentType, file.OpenReadStream());
        var savedThumbnail = await fileService.GenerateThumbnail(savedImg.path, thumbnailsPath, ThumbnailSize, ThumbnailSize);
        return (savedImg.path, savedThumbnail.path);
    }
}
