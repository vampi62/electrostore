using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Grpc;
using Grpc.Core;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ImgService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class ImgServiceTests : TestBase
    {
        private readonly Mock<IFileService> _fileService;

        public ImgServiceTests()
        {
            _fileService = new Mock<IFileService>();
        }

        private ImgService CreateService(ApplicationDbContext context)
        {
            return new ImgService(_mapper, context, _fileService.Object);
        }

        private static Items BuildItem(int id, string name = "Item")
        {
            return new Items
            {
                id_item = id,
                reference_name_item = "REF-" + id,
                friendly_name_item = name
            };
        }

        private static Imgs BuildImg(int id, int itemId, string name = "Image")
        {
            return new Imgs
            {
                id_img = id,
                id_item = itemId,
                nom_img = name,
                url_picture_img = "images/" + itemId + "/" + id + ".png",
                url_thumbnail_img = "imagesThumbnails/" + itemId + "/" + id + ".png"
            };
        }

        private static Mock<IFormFile> BuildFormFile(string fileName = "picture.png", string contentType = "image/png")
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
            return file;
        }

        // --- GetImgsByItemId ---

        [Fact]
        public async Task GetImgsByItemId_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetImgsByItemId(1);
            });
        }

        [Fact]
        public async Task GetImgsByItemId_ShouldReturnOnlyImagesForGivenItem()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.Imgs.Add(BuildImg(1, 1));
            context.Imgs.Add(BuildImg(2, 1));
            context.Imgs.Add(BuildImg(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetImgsByItemId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetImgById ---

        [Fact]
        public async Task GetImgById_ShouldReturnImage_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Imgs.Add(BuildImg(1, 1, "Front view"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetImgById(1);
            // Assert
            Assert.Equal("Front view", result.nom_img);
        }

        [Fact]
        public async Task GetImgById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetImgById(999);
            });
        }

        [Fact]
        public async Task GetImgById_ShouldThrowKeyNotFoundException_WhenItemIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.Imgs.Add(BuildImg(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetImgById(1, itemId: 2);
            });
        }

        // --- CreateImg ---

        [Fact]
        public async Task CreateImg_ShouldThrowKeyNotFoundException_WhenItemNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateImgDto { nom_img = "Front view", id_item = 999, img_file = BuildFormFile().Object };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreateImg(dto);
            });
        }

        [Fact]
        public async Task CreateImg_ShouldCreateImageAndThumbnail_WhenItemExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.SaveFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<bool>()))
                .ReturnsAsync(new SaveFileResult { path = "images/1/picture.png", mimeType = "image/png" });
            _fileService.Setup(f => f.GenerateThumbnail(It.IsAny<string>(), It.IsAny<string>(), 256, 256))
                .ReturnsAsync(new SaveFileResult { path = "imagesThumbnails/1/picture.png", mimeType = "image/png" });
            var service = CreateService(context);
            var dto = new CreateImgDto { nom_img = "Front view", id_item = 1, img_file = BuildFormFile().Object };
            // Act
            var result = await service.CreateImg(dto);
            // Assert
            Assert.Equal("Front view", result.nom_img);
            Assert.Equal("images/1/picture.png", result.url_picture_img);
            Assert.Equal("imagesThumbnails/1/picture.png", result.url_thumbnail_img);
            Assert.Equal(1, await context.Imgs.CountAsync());
        }

        // --- UpdateImg ---

        [Fact]
        public async Task UpdateImg_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateImg(999, new UpdateImgDto());
            });
        }

        [Fact]
        public async Task UpdateImg_ShouldThrowKeyNotFoundException_WhenItemIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.Imgs.Add(BuildImg(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateImg(1, new UpdateImgDto(), itemId: 2);
            });
        }

        [Fact]
        public async Task UpdateImg_ShouldUpdateFields_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Imgs.Add(BuildImg(1, 1, "Old name"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.UpdateImg(1, new UpdateImgDto { nom_img = "New name", description_img = "Updated" });
            // Assert
            Assert.Equal("New name", result.nom_img);
            Assert.Equal("Updated", result.description_img);
        }

        // --- DeleteImg ---

        [Fact]
        public async Task DeleteImg_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteImg(999);
            });
        }

        [Fact]
        public async Task DeleteImg_ShouldThrowKeyNotFoundException_WhenItemIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Items.Add(BuildItem(2));
            context.Imgs.Add(BuildImg(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteImg(1, itemId: 2);
            });
        }

        [Fact]
        public async Task DeleteImg_ShouldDeletePictureAndThumbnailFiles_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Imgs.Add(BuildImg(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeleteImg(1);
            // Assert
            Assert.Equal(0, await context.Imgs.CountAsync());
            _fileService.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Exactly(2));
        }

        // --- StreamTrainingImagesAsync ---

        [Fact]
        public async Task StreamTrainingImagesAsync_ShouldStreamAllImages_WhenNoRequestedSetProvided()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Imgs.Add(BuildImg(1, 1));
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.GetFile(It.IsAny<string>()))
                .ReturnsAsync(new GetFileResult { Success = true, MimeType = "image/png", FileStream = new MemoryStream(new byte[] { 1, 2, 3 }) });
            var responseStream = new Mock<IAsyncStreamWriter<TrainingImage>>();
            var writtenImages = new List<TrainingImage>();
            responseStream.Setup(r => r.WriteAsync(It.IsAny<TrainingImage>(), It.IsAny<CancellationToken>()))
                .Callback<TrainingImage, CancellationToken>((image, _) => writtenImages.Add(image))
                .Returns(Task.CompletedTask);
            var service = CreateService(context);
            // Act
            await service.StreamTrainingImagesAsync(responseStream.Object, null, CancellationToken.None);
            // Assert
            var image = Assert.Single(writtenImages);
            Assert.Equal("1", image.Label);
        }

        [Fact]
        public async Task StreamTrainingImagesAsync_ShouldSkipImages_InRequestedSet()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Imgs.Add(BuildImg(1, 1));
            await context.SaveChangesAsync();
            var responseStream = new Mock<IAsyncStreamWriter<TrainingImage>>();
            var writeCount = 0;
            responseStream.Setup(r => r.WriteAsync(It.IsAny<TrainingImage>(), It.IsAny<CancellationToken>()))
                .Callback(() => writeCount++)
                .Returns(Task.CompletedTask);
            var service = CreateService(context);
            var requestedSet = new HashSet<string> { "1.png" };
            // Act
            await service.StreamTrainingImagesAsync(responseStream.Object, requestedSet, CancellationToken.None);
            // Assert
            Assert.Equal(0, writeCount);
            _fileService.Verify(f => f.GetFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task StreamTrainingImagesAsync_ShouldSkipImage_WhenFileServiceFails()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Items.Add(BuildItem(1));
            context.Imgs.Add(BuildImg(1, 1));
            await context.SaveChangesAsync();
            _fileService.Setup(f => f.GetFile(It.IsAny<string>()))
                .ReturnsAsync(new GetFileResult { Success = false, MimeType = "image/png", ErrorMessage = "not found" });
            var responseStream = new Mock<IAsyncStreamWriter<TrainingImage>>();
            var writeCount = 0;
            responseStream.Setup(r => r.WriteAsync(It.IsAny<TrainingImage>(), It.IsAny<CancellationToken>()))
                .Callback(() => writeCount++)
                .Returns(Task.CompletedTask);
            var service = CreateService(context);
            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await service.StreamTrainingImagesAsync(responseStream.Object, null, CancellationToken.None);
            });
            // Assert
            Assert.Null(exception);
            Assert.Equal(0, writeCount);
        }
    }
}
