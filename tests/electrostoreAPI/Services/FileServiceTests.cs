using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using AutoMapper;
using Minio;
using Minio.DataModel.Args;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

using ElectrostoreAPI.Tests.Utils;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace ElectrostoreAPI.Tests.Services
{
    public class FileServiceTests : TestBase
    {
        private readonly Mock<IMinioClient> _minioClient;

        public FileServiceTests()
        {
            _minioClient = new Mock<IMinioClient>();
        }

        private FileService CreateService(IConfiguration configuration)
        {
            return new FileService(configuration, _minioClient.Object);
        }

        private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static IConfiguration BuildS3EnabledConfiguration(string bucketName = "test-bucket")
        {
            return BuildConfiguration(new Dictionary<string, string?>
            {
                ["S3:Enable"] = "true",
                ["S3:BucketName"] = bucketName
            });
        }

        private static IConfiguration BuildS3DisabledConfiguration()
        {
            return BuildConfiguration(new Dictionary<string, string?>
            {
                ["S3:Enable"] = "false"
            });
        }

        // Helper method to convert List to IAsyncEnumerable
        private async IAsyncEnumerable<T> AsyncEnumerable<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }

        // --- GetFile ---

        [Fact]
        public async Task GetFile_ShouldReturnS3File_WhenFileExists()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            _minioClient
                .Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
                .Callback<GetObjectArgs, CancellationToken>((args, ct) =>
                {
                    // Simule le comportement de Minio: invoque le callback avec un flux rempli
                    var data = Encoding.UTF8.GetBytes("contenu de test");
                    using var sourceStream = new MemoryStream(data);

                    // Récupère via réflexion le délégué de callback passé par WithCallbackStream
                    var argType = args.GetType();
                    Delegate? callback = null;

                    // Champs/propriétés potentiels selon versions du SDK Minio
                    var cbField = argType.GetField("CallbackAction", BindingFlags.Instance | BindingFlags.NonPublic)
                                  ?? argType.GetField("Callback", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (cbField != null)
                    {
                        callback = cbField.GetValue(args) as Delegate;
                    }
                    if (callback == null)
                    {
                        var cbProp = argType.GetProperty("CallbackAction", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                                   ?? argType.GetProperty("Callback", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (cbProp != null)
                        {
                            callback = cbProp.GetValue(args) as Delegate;
                        }
                    }
                    if (callback != null)
                    {
                        callback.DynamicInvoke(sourceStream);
                    }
                })
                .ReturnsAsync((Minio.DataModel.ObjectStat)null!);

            var fileService = CreateService(configuration);

            // Act
            var result = await fileService.GetFile("test-file.txt");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileStream);
            Assert.Equal("text/plain", result.MimeType);
        }

        [Fact]
        public async Task GetFile_ShouldReturnS3Error_WhenFileDoesNotExist()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            _minioClient.Setup(m => m.GetObjectAsync(It.IsAny<GetObjectArgs>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Minio.Exceptions.ObjectNotFoundException("File not found"));

            var fileService = CreateService(configuration);

            // Act
            var result = await fileService.GetFile("non-existent-file.txt");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("File not found", result.ErrorMessage);
        }

        [Fact]
        public async Task GetFile_ShouldReturnLocalFile_WhenS3DisabledAndFileExists()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);

            var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "test-file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
            await File.WriteAllTextAsync(testFilePath, "contenu de test local");

            // Act
            var result = await fileService.GetFile("test-file.txt");

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.FileStream);
            Assert.Equal("text/plain", result.MimeType);

            // Cleanup
            File.Delete(testFilePath);
        }

        [Fact]
        public async Task GetFile_ShouldReturnLocalError_WhenS3DisabledAndFileDoesNotExist()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);

            // Act
            var result = await fileService.GetFile("non-existent-local-file.txt");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("File not found", result.ErrorMessage);
        }

        // --- FileExists ---

        [Fact]
        public async Task FileExists_ShouldReturnTrue_WhenS3FileExists()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            _minioClient
                .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Minio.DataModel.ObjectStat)null!);

            var fileService = CreateService(configuration);

            // Act
            var exists = await fileService.FileExists("existing-s3-file.txt");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task FileExists_ShouldReturnFalse_WhenS3FileDoesNotExist()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            _minioClient
                .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Minio.Exceptions.ObjectNotFoundException("File not found"));

            var fileService = CreateService(configuration);

            // Act
            var exists = await fileService.FileExists("non-existent-s3-file.txt");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task FileExists_ShouldReturnTrue_WhenLocalFileExists()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);

            var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "existing-local-file.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
            await File.WriteAllTextAsync(testFilePath, "local file content");

            // Act
            var exists = await fileService.FileExists("existing-local-file.txt");

            // Assert
            Assert.True(exists);

            // Cleanup
            File.Delete(testFilePath);
        }

        [Fact]
        public async Task FileExists_ShouldReturnFalse_WhenLocalFileDoesNotExist()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);

            // Act
            var exists = await fileService.FileExists("non-existent-local-file.txt");

            // Assert
            Assert.False(exists);
        }

        // --- SaveFile ---

        [Fact]
        public async Task SaveFile_ShouldSaveS3File_WhenS3Enabled()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            _minioClient
                .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);
            _minioClient
                .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Minio.Exceptions.ObjectNotFoundException("File not found"));

            var fileService = CreateService(configuration);
            var content = "This is a test file";
            var fileName = "test-file.txt";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var result = await fileService.SaveFile("some-path", fileName, "text/plain", ms);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("some-path", result.path);
            Assert.Equal("text/plain", result.mimeType);
        }

        [Fact]
        public async Task SaveFile_ShouldSaveLocalFile_WhenS3Disabled()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);
            var content = "This is a test local file";
            var fileName = "local-test-file.txt";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // create the directory if it doesn't exist
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "local-path");
            Directory.CreateDirectory(saveDir);

            // Act
            var result = await fileService.SaveFile("local-path", fileName, "text/plain", ms);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("local-path", result.path);
            Assert.Equal("text/plain", result.mimeType);

            // Cleanup
            var savedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", result.path);
            if (File.Exists(savedFilePath))
            {
                File.Delete(savedFilePath);
            }
            if (Directory.Exists(saveDir) && !Directory.EnumerateFileSystemEntries(saveDir).Any())
            {
                Directory.Delete(saveDir);
            }
        }

        [Fact]
        public async Task SaveFile_ShouldPreventDirectoryTraversal()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);
            var content = "This is a test file for directory traversal";
            var fileName = "../traversal-test-file.txt"; // Attempted directory traversal
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // create the directory if it doesn't exist
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "safe-path");
            Directory.CreateDirectory(saveDir);

            // Act
            var result = await fileService.SaveFile("safe-path", fileName, "text/plain", ms);

            // Assert
            Assert.NotNull(result);
            Assert.DoesNotContain("..", result.path); // Ensure no directory traversal in saved path

            // Cleanup
            var savedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", result.path);
            if (File.Exists(savedFilePath))
            {
                File.Delete(savedFilePath);
            }
            if (Directory.Exists(saveDir) && !Directory.EnumerateFileSystemEntries(saveDir).Any())
            {
                Directory.Delete(saveDir);
            }
        }

        [Fact]
        public async Task SaveFile_ShouldHandleLongFileNames()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);
            var content = "This is a test file for long file names";
            var longFileName = new string('a', 150) + ".txt"; // 150 characters long
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // create the directory if it doesn't exist
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "long-name-path");
            Directory.CreateDirectory(saveDir);

            // Act
            var result = await fileService.SaveFile("long-name-path", longFileName, "text/plain", ms);

            // Assert
            Assert.NotNull(result);
            var savedFileName = Path.GetFileName(result.path);
            Assert.True(savedFileName.Length <= 104); // 100 chars + 4 for ".txt"

            // Cleanup
            var savedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", result.path);
            if (File.Exists(savedFilePath))
            {
                File.Delete(savedFilePath);
            }
            if (Directory.Exists(saveDir) && !Directory.EnumerateFileSystemEntries(saveDir).Any())
            {
                Directory.Delete(saveDir);
            }
        }

        [Fact]
        public async Task SaveFile_ShouldHandleS3UploadError()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            _minioClient
                .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("S3 upload failed"));
            _minioClient
                .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Minio.Exceptions.ObjectNotFoundException("File not found"));

            var fileService = CreateService(configuration);
            var content = "This is a test file for S3 upload error";
            var fileName = "s3-error-test-file.txt";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await fileService.SaveFile("error-path", fileName, "text/plain", ms);
            });
        }

        [Fact]
        public async Task SaveFile_ShouldHandleLocalFileSaveError()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);

            // Simulate error using a stream that throws on CopyToAsync
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "error-path");
            Directory.CreateDirectory(savePath);
            var throwingStream = new ThrowingStream(new UnauthorizedAccessException("Cannot write to read-only directory"));

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await fileService.SaveFile("error-path", "local-error-test-file.txt", "text/plain", throwingStream);
            });

            // Cleanup
            Directory.Delete(savePath, true);
        }

        [Fact]
        public async Task SaveFile_ShouldHandleFileNameCollisions_InS3()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            int callCount = 0;
            _minioClient
                .Setup(m => m.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount < 3)
                    {
                        // Simulate file exists - return a completed task with null (file exists but we don't need the stat)
                        return Task.FromResult<Minio.DataModel.ObjectStat>(null!);
                    }
                    else
                    {
                        // Simulate file does not exist
                        throw new Minio.Exceptions.ObjectNotFoundException("File not found");
                    }
                });

            _minioClient
                .Setup(m => m.PutObjectAsync(It.IsAny<PutObjectArgs>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Minio.DataModel.Response.PutObjectResponse)null!);

            var fileService = CreateService(configuration);
            var content = "This is a test file for S3 name collision";
            var fileName = "collision-test-file.txt";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            var result = await fileService.SaveFile("collision-path", fileName, "text/plain", ms);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("collision-path", result.path);
            Assert.Matches(@"collision-test-file\(\d+\)\.txt$", result.path); // Check for appended number in filename
        }

        [Fact]
        public async Task SaveFile_ShouldHandleFileNameCollisions_InLocalStorage()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);
            var content = "This is a test file for local name collision";
            var fileName = "local-collision-test-file.txt";
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Pre-create a file to cause a collision
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "local-collision-path");
            Directory.CreateDirectory(saveDir);
            var existingFilePath = Path.Combine(saveDir, fileName);
            await File.WriteAllTextAsync(existingFilePath, "existing file content");

            // Act
            var result = await fileService.SaveFile("local-collision-path", fileName, "text/plain", ms);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("local-collision-path", result.path);
            Assert.Matches(@"local-collision-test-file\(\d+\)\.txt$", result.path); // Check for appended number in filename

            // Cleanup
            var savedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", result.path);
            if (File.Exists(savedFilePath))
            {
                File.Delete(savedFilePath);
            }
            if (File.Exists(existingFilePath))
            {
                File.Delete(existingFilePath);
            }
            if (Directory.Exists(saveDir))
            {
                Directory.Delete(saveDir);
            }
        }

        // --- GenerateThumbnail ---

        [Fact]
        public async Task GenerateThumbnail_ShouldGenerateThumbnail_ForImageFile()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);
            // create directory for thumbnails
            var thumbnailsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "thumbnailsTest");
            var thumbnailsDirIn = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "thumbnailsTest", "main");
            var thumbnailsDirOut = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "thumbnailsTest", "thumbnails");
            Directory.CreateDirectory(thumbnailsDir);
            Directory.CreateDirectory(thumbnailsDirIn);
            Directory.CreateDirectory(thumbnailsDirOut);

            // Create a simple image in memory and save it to create a real file
            var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(200, 200);
            var imageMs = new MemoryStream();
            await image.SaveAsJpegAsync(imageMs);
            imageMs.Position = 0;

            var fileName = "test-image.jpg";
            imageMs.Position = 0;

            // Save the original image first
            var saveResult = await fileService.SaveFile(Path.Combine("thumbnailsTest", "main"), fileName, "image/jpeg", imageMs);
            var sourceFilePath = saveResult.path;
            var destPath = Path.Combine("thumbnailsTest","thumbnails");

            // Act
            var thumbnailResult = await fileService.GenerateThumbnail(sourceFilePath, destPath, 100, 100);

            // Assert
            Assert.NotNull(thumbnailResult);
            Assert.Contains("main", saveResult.path);
            Assert.Contains("thumbnails", thumbnailResult.path);
            Assert.Equal("image/jpeg", thumbnailResult.mimeType);

            // Cleanup
            var savedThumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", thumbnailResult.path);
            if (File.Exists(savedThumbnailPath))
            {
                File.Delete(savedThumbnailPath);
            }
            var savedOriginalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", sourceFilePath);
            if (File.Exists(savedOriginalPath))
            {
                File.Delete(savedOriginalPath);
            }
            if (Directory.Exists(thumbnailsDir) && !Directory.EnumerateFileSystemEntries(thumbnailsDir).Any())
            {
                Directory.Delete(thumbnailsDir);
            }
        }

        // --- DeleteFile ---

        [Fact]
        public async Task DeleteFile_ShouldDeleteS3File_WhenS3Enabled()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            _minioClient
                .Setup(m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var fileService = CreateService(configuration);

            // Act
            await fileService.DeleteFile("test-file-to-delete.txt");

            // Assert
            _minioClient.Verify(m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteFile_ShouldDeleteLocalFile_WhenS3Disabled()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);

            var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "file-to-delete.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
            await File.WriteAllTextAsync(testFilePath, "file content to delete");

            // Act
            await fileService.DeleteFile("file-to-delete.txt");

            // Assert
            Assert.False(File.Exists(testFilePath));
        }

        // --- CreateDirectory ---

        [Fact]
        public async Task CreateDirectory_ShouldCreateLocalDirectory_WhenS3Disabled()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);

            var dirPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "new-directory");

            // Act
            await fileService.CreateDirectory("new-directory");

            // Assert
            Assert.True(Directory.Exists(dirPath));

            // Cleanup
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath);
            }
        }

        // --- DeleteDirectory ---

        [Fact]
        public async Task DeleteDirectory_ShouldDeleteLocalDirectory_WhenS3Disabled()
        {
            // Arrange
            var configuration = BuildS3DisabledConfiguration();

            var fileService = CreateService(configuration);

            var dirPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "directory-to-delete");
            Directory.CreateDirectory(dirPath);

            // Act
            await fileService.DeleteDirectory("directory-to-delete");

            // Assert
            Assert.False(Directory.Exists(dirPath));
        }

        [Fact]
        public async Task DeleteDirectory_ShouldDeleteS3Directory_WhenS3Enabled()
        {
            // Arrange
            var configuration = BuildS3EnabledConfiguration();

            var fileService = CreateService(configuration);

            var objectsToDelete = new List<Minio.DataModel.Item>
            {
                new Minio.DataModel.Item { Key = "directory-to-delete/file1.txt" },
                new Minio.DataModel.Item { Key = "directory-to-delete/file2.txt" }
            };

            _minioClient
                .Setup(m => m.ListObjectsEnumAsync(It.IsAny<ListObjectsArgs>(), It.IsAny<CancellationToken>()))
                .Returns(AsyncEnumerable(objectsToDelete));

            _minioClient
                .Setup(m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await fileService.DeleteDirectory("directory-to-delete");

            // Assert
            _minioClient.Verify(m => m.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        private sealed class ThrowingStream : MemoryStream
        {
            private readonly Exception _exception;
            public ThrowingStream(Exception exception) { _exception = exception; }
            public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
                => throw _exception;
        }
    }
}
