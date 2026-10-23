using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.FileService;
using Moq;
using SkiaSharp;

namespace ElectrostoreAPI.Tests.Utils
{
    public static class FileServiceMockExtensions
    {
        /// <summary>
        /// IFileService.GenerateThumbnail is an extension method built on GetFile/SaveFile, so it cannot be mocked:
        /// mock those two instead. The picture is saved at <paramref name="picturePath"/>, and anything saved in a
        /// "...Thumbnails" folder is returned at <paramref name="thumbnailPath"/>.
        /// </summary>
        public static void SetupImageWithThumbnailSaving(this Mock<IFileService> fileService, string picturePath, string thumbnailPath)
        {
            fileService
                .Setup(f => f.SaveFile(It.Is<string>(p => !p.Contains("Thumbnails")), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = picturePath, mime_type = "image/png" });
            fileService
                .Setup(f => f.SaveFile(It.Is<string>(p => p.Contains("Thumbnails")), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = thumbnailPath, mime_type = "image/jpeg" });
            fileService
                .Setup(f => f.GetFile(picturePath))
                .Returns(() => Task.FromResult(new GetFileResult { success = true, file_stream = CreatePngStream(), mime_type = "image/png" }));
        }

        private static MemoryStream CreatePngStream()
        {
            using var bitmap = new SKBitmap(8, 8);
            using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            var stream = new MemoryStream();
            data.SaveTo(stream);
            stream.Position = 0;
            return stream;
        }
    }
}
