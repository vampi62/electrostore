using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.UserPushSubscriptionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class UserPushSubscriptionServiceTests : TestBase
    {
        private readonly Mock<IKafkaProducerService> _kafkaProducerService;

        public UserPushSubscriptionServiceTests()
        {
            _kafkaProducerService = new Mock<IKafkaProducerService>();
        }

        private UserPushSubscriptionService CreateService(ApplicationDbContext context, IConfiguration? configuration = null)
        {
            configuration ??= new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            return new UserPushSubscriptionService(context, _mapper, configuration, _kafkaProducerService.Object);
        }

        private static Users BuildUser(int id)
        {
            return new Users
            {
                id_user = id,
                nom_user = "Nom",
                prenom_user = "Prenom",
                email_user = "user" + id + "@test.com",
                mdp_user = "hashedpassword"
            };
        }

        private static UserPushSubscriptions BuildSubscription(int id, int userId, string endpoint = "https://push.example.com/endpoint")
        {
            return new UserPushSubscriptions
            {
                id_push_subscription = id,
                id_user = userId,
                endpoint = endpoint,
                p256dh = "p256dh-key",
                auth = "auth-key"
            };
        }

        // --- GetPushSubscriptionsByUserId ---

        [Fact]
        public async Task GetPushSubscriptionsByUserId_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetPushSubscriptionsByUserId(1);
            });
        }

        [Fact]
        public async Task GetPushSubscriptionsByUserId_ShouldReturnOnlySubscriptionsForGivenUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            context.Users.Add(BuildUser(2));
            context.UserPushSubscriptions.Add(BuildSubscription(1, 1));
            context.UserPushSubscriptions.Add(BuildSubscription(2, 1));
            context.UserPushSubscriptions.Add(BuildSubscription(3, 2));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetPushSubscriptionsByUserId(1);
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        // --- GetPushSubscriptionById ---

        [Fact]
        public async Task GetPushSubscriptionById_ShouldReturnSubscription_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            context.UserPushSubscriptions.Add(BuildSubscription(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetPushSubscriptionById(1);
            // Assert
            Assert.Equal(1, result.id_user);
        }

        [Fact]
        public async Task GetPushSubscriptionById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetPushSubscriptionById(999);
            });
        }

        [Fact]
        public async Task GetPushSubscriptionById_ShouldThrowKeyNotFoundException_WhenUserIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            context.Users.Add(BuildUser(2));
            context.UserPushSubscriptions.Add(BuildSubscription(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetPushSubscriptionById(1, userId: 2);
            });
        }

        // --- CreatePushSubscription ---

        [Fact]
        public async Task CreatePushSubscription_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateUserPushSubscriptionDto { id_user = 999, endpoint = "https://push.example.com/e", p256dh = "k", auth = "a" };
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.CreatePushSubscription(dto);
            });
        }

        [Fact]
        public async Task CreatePushSubscription_ShouldCreateNewSubscription_WhenEndpointNotAlreadyRegistered()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateUserPushSubscriptionDto { id_user = 1, endpoint = "https://push.example.com/e", p256dh = "k", auth = "a" };
            // Act
            var result = await service.CreatePushSubscription(dto);
            // Assert
            Assert.Equal(1, result.id_user);
            Assert.Equal(1, await context.UserPushSubscriptions.CountAsync());
        }

        [Fact]
        public async Task CreatePushSubscription_ShouldUpsertExistingSubscription_WhenEndpointAlreadyRegistered()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            context.UserPushSubscriptions.Add(BuildSubscription(1, 1, "https://push.example.com/e"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateUserPushSubscriptionDto { id_user = 1, endpoint = "https://push.example.com/e", p256dh = "new-key", auth = "new-auth", device_name = "Phone" };
            // Act
            var result = await service.CreatePushSubscription(dto);
            // Assert
            Assert.Equal("new-key", result.p256dh);
            Assert.Equal("Phone", result.device_name);
            Assert.Equal(1, await context.UserPushSubscriptions.CountAsync());
        }

        // --- DeletePushSubscription ---

        [Fact]
        public async Task DeletePushSubscription_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeletePushSubscription(999);
            });
        }

        [Fact]
        public async Task DeletePushSubscription_ShouldThrowKeyNotFoundException_WhenUserIdDoesNotMatch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            context.Users.Add(BuildUser(2));
            context.UserPushSubscriptions.Add(BuildSubscription(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeletePushSubscription(1, userId: 2);
            });
        }

        [Fact]
        public async Task DeletePushSubscription_ShouldDeleteSubscription_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            context.UserPushSubscriptions.Add(BuildSubscription(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.DeletePushSubscription(1);
            // Assert
            Assert.Equal(0, await context.UserPushSubscriptions.CountAsync());
        }

        // --- GetPushSubscriptionsByUserIdAsync ---

        [Fact]
        public async Task GetPushSubscriptionsByUserIdAsync_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetPushSubscriptionsByUserIdAsync(1);
            });
        }

        [Fact]
        public async Task GetPushSubscriptionsByUserIdAsync_ShouldReturnSubscriptions_WhenUserExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            context.UserPushSubscriptions.Add(BuildSubscription(1, 1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetPushSubscriptionsByUserIdAsync(1);
            // Assert
            Assert.Single(result);
        }

        // --- SendTestPushNotification ---

        [Fact]
        public async Task SendTestPushNotification_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.SendTestPushNotification(999);
            });
        }

        [Fact]
        public async Task SendTestPushNotification_ShouldPublishNotification_WhenUserExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.SendTestPushNotification(1);
            // Assert
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "user-1-push-test", It.IsAny<string>(), default), Times.Once);
        }

        // --- SendTestEmailNotification ---

        [Fact]
        public async Task SendTestEmailNotification_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.SendTestEmailNotification(999);
            });
        }

        [Fact]
        public async Task SendTestEmailNotification_ShouldPublishNotification_WhenUserExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            await service.SendTestEmailNotification(1);
            // Assert
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "user-1-email-test", It.IsAny<string>(), default), Times.Once);
        }
    }
}
