using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text.Json;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.UserPushSubscriptionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class UserPushSubscriptionServiceTests : TestBase
    {
        private const string NotificationTopic = "notification-requests";

        private readonly Mock<IKafkaProducerService> _kafkaProducerService = new();

        private UserPushSubscriptionService CreateService(ApplicationDbContext context, string? language = null)
        {
            var values = new Dictionary<string, string?>();
            if (language is not null)
            {
                values["AppLanguage"] = language;
            }
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            return new UserPushSubscriptionService(context, _mapper, configuration, _kafkaProducerService.Object);
        }

        private static Users BuildUser(string email = "user@example.com") => new()
        {
            name_user = "Doe",
            firstname_user = "John",
            email_user = email,
        };

        private static UserPushSubscriptions BuildSubscription(int userId, string endpoint = "https://push/1") => new()
        {
            id_user = userId,
            endpoint = endpoint,
            p256dh = "key",
            auth = "auth",
            device_name = "phone"
        };

        private static CreateUserPushSubscriptionDto BuildCreateDto(int userId, string endpoint = "https://push/1") => new()
        {
            id_user = userId,
            endpoint = endpoint,
            p256dh = "key",
            auth = "auth",
            device_name = "phone"
        };

        private static async Task<Users> SeedUser(ApplicationDbContext context, string email = "user@example.com")
        {
            var user = BuildUser(email);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        // --- GetPushSubscriptionsByUserId ---

        [Fact]
        public async Task GetPushSubscriptionsByUserId_ShouldReturnOnlySubscriptionsOfUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var other = await SeedUser(context, "other@example.com");
            context.UserPushSubscriptions.AddRange(
                BuildSubscription(user.id_user, "https://push/a"),
                BuildSubscription(user.id_user, "https://push/b"),
                BuildSubscription(other.id_user, "https://push/c"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetPushSubscriptionsByUserId(user.id_user);

            Assert.Equal(2, result.data.Count());
            Assert.All(result.data, s => Assert.Equal(user.id_user, s.id_user));
        }

        [Fact]
        public async Task GetPushSubscriptionsByUserId_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetPushSubscriptionsByUserId(999));
        }

        // --- GetPushSubscriptionById ---

        [Fact]
        public async Task GetPushSubscriptionById_ShouldReturnSubscription_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var subscription = BuildSubscription(user.id_user);
            context.UserPushSubscriptions.Add(subscription);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetPushSubscriptionById(subscription.id_user_push_subscription, user.id_user);

            Assert.Equal("https://push/1", result.endpoint);
        }

        [Fact]
        public async Task GetPushSubscriptionById_ShouldThrowKeyNotFoundException_WhenUserDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var subscription = BuildSubscription(user.id_user);
            context.UserPushSubscriptions.Add(subscription);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetPushSubscriptionById(subscription.id_user_push_subscription, user.id_user + 1));
        }

        [Fact]
        public async Task GetPushSubscriptionById_ShouldThrowKeyNotFoundException_WhenSubscriptionDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetPushSubscriptionById(999));
        }

        // --- CreatePushSubscription ---

        [Fact]
        public async Task CreatePushSubscription_ShouldPersistNewSubscription()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var service = CreateService(context);

            var result = await service.CreatePushSubscription(BuildCreateDto(user.id_user));

            Assert.Equal("https://push/1", result.endpoint);
            Assert.Equal(1, await context.UserPushSubscriptions.CountAsync());
        }

        [Fact]
        public async Task CreatePushSubscription_ShouldUpdateExistingSubscription_WhenEndpointAlreadyRegisteredForUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            context.UserPushSubscriptions.Add(BuildSubscription(user.id_user));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.CreatePushSubscription(new CreateUserPushSubscriptionDto
            {
                id_user = user.id_user,
                endpoint = "https://push/1",
                p256dh = "new-key",
                auth = "new-auth",
                device_name = "tablet"
            });

            Assert.Equal("new-key", result.p256dh);
            Assert.Equal("new-auth", result.auth);
            Assert.Equal("tablet", result.device_name);
            Assert.Equal(1, await context.UserPushSubscriptions.CountAsync());
        }

        [Fact]
        public async Task CreatePushSubscription_ShouldCreateSecondSubscription_WhenSameEndpointBelongsToAnotherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var other = await SeedUser(context, "other@example.com");
            context.UserPushSubscriptions.Add(BuildSubscription(other.id_user));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.CreatePushSubscription(BuildCreateDto(user.id_user));

            Assert.Equal(2, await context.UserPushSubscriptions.CountAsync());
        }

        [Fact]
        public async Task CreatePushSubscription_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreatePushSubscription(BuildCreateDto(999)));
        }

        // --- DeletePushSubscription ---

        [Fact]
        public async Task DeletePushSubscription_ShouldRemoveSubscription()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var subscription = BuildSubscription(user.id_user);
            context.UserPushSubscriptions.Add(subscription);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await service.DeletePushSubscription(subscription.id_user_push_subscription, user.id_user);

            Assert.Equal(0, await context.UserPushSubscriptions.CountAsync());
        }

        [Fact]
        public async Task DeletePushSubscription_ShouldThrowKeyNotFoundException_WhenUserDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var subscription = BuildSubscription(user.id_user);
            context.UserPushSubscriptions.Add(subscription);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeletePushSubscription(subscription.id_user_push_subscription, user.id_user + 1));
            Assert.Equal(1, await context.UserPushSubscriptions.CountAsync());
        }

        [Fact]
        public async Task DeletePushSubscription_ShouldThrowKeyNotFoundException_WhenSubscriptionDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeletePushSubscription(999));
        }

        // --- GetPushSubscriptionsByUserIdAsync ---

        [Fact]
        public async Task GetPushSubscriptionsByUserIdAsync_ShouldReturnSubscriptionsOfUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var other = await SeedUser(context, "other@example.com");
            context.UserPushSubscriptions.AddRange(
                BuildSubscription(user.id_user, "https://push/a"),
                BuildSubscription(other.id_user, "https://push/b"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetPushSubscriptionsByUserIdAsync(user.id_user);

            var single = Assert.Single(result);
            Assert.Equal("https://push/a", single.endpoint);
        }

        [Fact]
        public async Task GetPushSubscriptionsByUserIdAsync_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetPushSubscriptionsByUserIdAsync(999));
        }

        // --- SendTestPushNotification / SendTestEmailNotification ---

        [Fact]
        public async Task SendTestPushNotification_ShouldPublishWebPushNotification()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var service = CreateService(context, language: "fr");
            string? payload = null;
            _kafkaProducerService
                .Setup(k => k.PublishAsync(NotificationTopic, $"user-{user.id_user}-push-test", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((_, _, message, _) => payload = message)
                .Returns(Task.CompletedTask);

            await service.SendTestPushNotification(user.id_user);

            _kafkaProducerService.VerifyAll();
            var message = JsonSerializer.Deserialize<NotificationMessage>(payload!)!;
            Assert.Equal(["webPush"], message.Types);
            Assert.Equal(user.id_user, message.RecipientUserId);
            Assert.Equal("fr", message.Language);
            Assert.False(string.IsNullOrEmpty(message.Title));
            Assert.False(string.IsNullOrEmpty(message.Body));
        }

        [Fact]
        public async Task SendTestEmailNotification_ShouldPublishEmailNotification_WithDefaultLanguage()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = await SeedUser(context);
            var service = CreateService(context);
            string? payload = null;
            _kafkaProducerService
                .Setup(k => k.PublishAsync(NotificationTopic, $"user-{user.id_user}-email-test", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((_, _, message, _) => payload = message)
                .Returns(Task.CompletedTask);

            await service.SendTestEmailNotification(user.id_user);

            _kafkaProducerService.VerifyAll();
            var message = JsonSerializer.Deserialize<NotificationMessage>(payload!)!;
            Assert.Equal(["email"], message.Types);
            Assert.Equal(user.id_user, message.RecipientUserId);
            Assert.Equal("en", message.Language);
        }

        [Fact]
        public async Task SendTestPushNotification_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SendTestPushNotification(999));
            _kafkaProducerService.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SendTestEmailNotification_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SendTestEmailNotification(999));
            _kafkaProducerService.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
