using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Grpc.Core;
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
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.IAService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class IAServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;
        private readonly Mock<IFileService> _fileService;
        private readonly Mock<IaCmdGrpc.IaCmdGrpcClient> _iaGrpcClient;
        private readonly Mock<IKafkaProducerService> _kafkaProducer;

        public IAServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _fileService = new Mock<IFileService>();
            _iaGrpcClient = new Mock<IaCmdGrpc.IaCmdGrpcClient>();
            _kafkaProducer = new Mock<IKafkaProducerService>();
        }

        private IAService CreateService(ApplicationDbContext context, IConfiguration? configuration = null)
        {
            configuration ??= new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            return new IAService(_mapper, context, _sessionService.Object, _fileService.Object, _iaGrpcClient.Object, _kafkaProducer.Object, configuration);
        }

        private void SetClientRole(UserRole role)
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        private static Models.IA BuildIA(int id, string name = "Model", bool trained = false)
        {
            return new Models.IA
            {
                id_ia = id,
                nom_ia = name,
                trained_ia = trained
            };
        }

        private static AsyncUnaryCall<TResponse> CreateAsyncUnaryCall<TResponse>(TResponse response)
        {
            return new AsyncUnaryCall<TResponse>(
                Task.FromResult(response),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }

        private static Mock<IFormFile> BuildFormFile()
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[] { 1, 2, 3 }));
            return file;
        }

        // --- GetIA ---

        [Fact]
        public async Task GetIA_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1));
            context.IA.Add(BuildIA(2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetIA();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetIA_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1));
            context.IA.Add(BuildIA(2));
            context.IA.Add(BuildIA(3));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetIA(idResearch: new List<int> { 2 });
            // Assert
            var ia = Assert.Single(result.data);
            Assert.Equal(2, ia.id_ia);
        }

        // --- GetIAById ---

        [Fact]
        public async Task GetIAById_ShouldReturnIA_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1, "My model"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetIAById(1);
            // Assert
            Assert.Equal("My model", result.nom_ia);
        }

        [Fact]
        public async Task GetIAById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetIAById(999);
            });
        }

        // --- CreateIA ---

        [Fact]
        public async Task CreateIA_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Moderator);
            var service = CreateService(context);
            var dto = new CreateIADto { nom_ia = "Model" };
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateIA(dto);
            });
        }

        [Fact]
        public async Task CreateIA_ShouldCreateIA_WhenAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateIADto { nom_ia = "Model" };
            // Act
            var result = await service.CreateIA(dto);
            // Assert
            Assert.Equal("Model", result.nom_ia);
            Assert.Equal(1, await context.IA.CountAsync());
        }

        // --- UpdateIA ---

        [Fact]
        public async Task UpdateIA_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Moderator);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UpdateIA(1, new UpdateIADto());
            });
        }

        [Fact]
        public async Task UpdateIA_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateIA(999, new UpdateIADto());
            });
        }

        [Fact]
        public async Task UpdateIA_ShouldUpdateFields_WhenAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1, "Old name"));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            var result = await service.UpdateIA(1, new UpdateIADto { nom_ia = "New name" });
            // Assert
            Assert.Equal("New name", result.nom_ia);
        }

        // --- DeleteIA ---

        [Fact]
        public async Task DeleteIA_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Moderator);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteIA(1);
            });
        }

        [Fact]
        public async Task DeleteIA_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteIA(999);
            });
        }

        [Fact]
        public async Task DeleteIA_ShouldDeleteAndPublishEvent_WhenAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            await service.DeleteIA(1);
            // Assert
            Assert.Equal(0, await context.IA.CountAsync());
            _kafkaProducer.Verify(k => k.PublishAsync("ia-requests", "1", It.IsAny<string>(), default), Times.Once);
        }

        // --- GetIATrainingStatusById ---

        [Fact]
        public async Task GetIATrainingStatusById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetIATrainingStatusById(999);
            });
        }

        [Fact]
        public async Task GetIATrainingStatusById_ShouldReturnStatus_WhenGrpcSucceeds()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1));
            await context.SaveChangesAsync();
            _iaGrpcClient
                .Setup(c => c.GetStatusAsync(It.Is<StatusRequest>(r => r.IdModel == 1), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Returns(CreateAsyncUnaryCall(new StatusReply { Status = "training", Message = "in progress", Epoch = 3, Accuracy = 0.8f, ValAccuracy = 0.75f, Loss = 0.2f, ValLoss = 0.25f }));
            var service = CreateService(context);
            // Act
            var result = await service.GetIATrainingStatusById(1);
            // Assert
            Assert.Equal("training", result.Status);
            Assert.Equal(3, result.Epoch);
            // Verify the grpc call was actually made with the requested model id (a mismatched
            // request would fall through to no Setup match and surface as a null-reply failure).
            _iaGrpcClient.Verify(c => c.GetStatusAsync(It.Is<StatusRequest>(r => r.IdModel == 1), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetIATrainingStatusById_ShouldReturnUnknownStatus_WhenGrpcThrows()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1));
            await context.SaveChangesAsync();
            _iaGrpcClient
                .Setup(c => c.GetStatusAsync(It.IsAny<StatusRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Throws(new RpcException(new Status(StatusCode.Unavailable, "down")));
            var service = CreateService(context);
            // Act
            var result = await service.GetIATrainingStatusById(1);
            // Assert
            Assert.Equal("unknown", result.Status);
        }

        // --- StartIATrainById ---

        [Fact]
        public async Task StartIATrainById_ShouldThrowUnauthorizedAccessException_WhenNotAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Moderator);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.StartIATrainById(1);
            });
        }

        [Fact]
        public async Task StartIATrainById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.StartIATrainById(999);
            });
        }

        [Fact]
        public async Task StartIATrainById_ShouldPublishTrainRequest_WhenAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1));
            await context.SaveChangesAsync();
            SetClientRole(UserRole.Admin);
            var service = CreateService(context);
            // Act
            await service.StartIATrainById(1);
            // Assert
            _kafkaProducer.Verify(k => k.PublishAsync("ia-requests", "1", It.IsAny<string>(), default), Times.Once);
        }

        // --- IADetectItem ---

        [Fact]
        public async Task IADetectItem_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new DetecDto { img_file = BuildFormFile().Object };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.IADetectItem(999, dto);
            });
        }

        [Fact]
        public async Task IADetectItem_ShouldThrowInvalidOperationException_WhenNotTrained()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1, trained: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new DetecDto { img_file = BuildFormFile().Object };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.IADetectItem(1, dto);
            });
        }

        [Fact]
        public async Task IADetectItem_ShouldReturnPrediction_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1, trained: true));
            await context.SaveChangesAsync();
            _iaGrpcClient
                .Setup(c => c.DetectAsync(It.Is<DetectRequest>(r => r.IdModel == 1 && r.ImageData.ToByteArray().SequenceEqual(new byte[] { 1, 2, 3 })), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Returns(CreateAsyncUnaryCall(new DetectReply { PredictedClass = 3, Confidence = 0.95f }));
            var service = CreateService(context);
            var dto = new DetecDto { img_file = BuildFormFile().Object };
            // Act
            var result = await service.IADetectItem(1, dto);
            // Assert
            Assert.Equal(3, result.PredictedLabel);
            Assert.Equal(0.95f, result.Score);
            // Verify the grpc call carried the requested model id and the uploaded image bytes.
            _iaGrpcClient.Verify(c => c.DetectAsync(It.Is<DetectRequest>(r => r.IdModel == 1), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IADetectItem_ShouldThrowInvalidOperationException_WhenGrpcThrows()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1, trained: true));
            await context.SaveChangesAsync();
            _iaGrpcClient
                .Setup(c => c.DetectAsync(It.IsAny<DetectRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Throws(new RpcException(new Status(StatusCode.Internal, "boom")));
            var service = CreateService(context);
            var dto = new DetecDto { img_file = BuildFormFile().Object };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.IADetectItem(1, dto);
            });
        }

        // --- UpdateIaStatusAsync ---

        [Fact]
        public async Task UpdateIaStatusAsync_ShouldReturnFalse_WhenIaNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var status = new IAStatusDto { Status = "training_completed", Message = "" };
            // Act
            var result = await service.UpdateIaStatusAsync(999, status, null, CancellationToken.None);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateIaStatusAsync_ShouldSetTrainedTrue_WhenTrainingCompleted()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1, trained: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var status = new IAStatusDto { Status = "training_completed", Message = "", Accuracy = 0.9f, ValAccuracy = 0.85f, Loss = 0.1f, ValLoss = 0.15f, Epoch = 10 };
            // Act
            var result = await service.UpdateIaStatusAsync(1, status, null, CancellationToken.None);
            // Assert
            Assert.True(result);
            var ia = await context.IA.FindAsync(1);
            Assert.True(ia!.trained_ia);
            Assert.NotNull(ia.date_training_ia);
        }

        [Fact]
        public async Task UpdateIaStatusAsync_ShouldNotChangeTrainedFlag_WhenTrainingFailed()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1, trained: false));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var status = new IAStatusDto { Status = "training_failed", Message = "error" };
            // Act
            var result = await service.UpdateIaStatusAsync(1, status, null, CancellationToken.None);
            // Assert
            Assert.True(result);
            var ia = await context.IA.FindAsync(1);
            Assert.False(ia!.trained_ia);
        }

        [Fact]
        public async Task UpdateIaStatusAsync_ShouldSetTrainedFalse_WhenTrainingStarted()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1, trained: true));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var status = new IAStatusDto { Status = "training_started", Message = "" };
            // Act
            var result = await service.UpdateIaStatusAsync(1, status, null, CancellationToken.None);
            // Assert
            Assert.True(result);
            var ia = await context.IA.FindAsync(1);
            Assert.False(ia!.trained_ia);
            Assert.Null(ia.date_training_ia);
        }

        [Fact]
        public async Task UpdateIaStatusAsync_ShouldReturnFalse_WhenUnknownStatus()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var status = new IAStatusDto { Status = "something_else", Message = "" };
            // Act
            var result = await service.UpdateIaStatusAsync(1, status, null, CancellationToken.None);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateIaStatusAsync_ShouldPublishNotification_WhenTrainingCompletedWithRequester()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.IA.Add(BuildIA(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var status = new IAStatusDto { Status = "training_completed", Message = "", Accuracy = 0.9f, ValAccuracy = 0.85f, Loss = 0.1f, ValLoss = 0.15f, Epoch = 10 };
            // Act
            await service.UpdateIaStatusAsync(1, status, requestedBy: 42, CancellationToken.None);
            // Assert
            _kafkaProducer.Verify(k => k.PublishAsync("notification-requests", "42-ia-training-status", It.IsAny<string>(), CancellationToken.None), Times.Once);
        }
    }
}
