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
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class UserServiceTests : TestBase
    {
        private readonly Mock<IKafkaProducerService> _kafkaProducerService = new();
        private readonly Mock<ISessionService> _sessionService = new();
        private readonly Mock<IJwiService> _jwiService = new();

        public UserServiceTests()
        {
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
        }

        private UserService CreateService(ApplicationDbContext context) =>
            new(_mapper, context, new ConfigurationBuilder().Build(), _kafkaProducerService.Object, _sessionService.Object, _jwiService.Object, CreateLogger<UserService>());

        private static Users BuildUser(string email, UserRole role = UserRole.User) => new()
        {
            name_user = "Nom",
            firstname_user = "Prenom",
            email_user = email,
            password_user = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            role_user = role
        };

        private static CreateUserDto BuildCreateDto(string email, UserRole role = UserRole.User) => new()
        {
            name_user = "Nom",
            firstname_user = "Prenom",
            email_user = email,
            password_user = "Password1!",
            role_user = role
        };

        // --- GetUsers ---

        [Fact]
        public async Task GetUsers_ShouldReturnAllUsers_WhenNoFilterApplied()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.AddRange(BuildUser("a@test.com"), BuildUser("b@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetUsers();

            Assert.Equal(2, result.pagination.total);
        }

        // --- GetUserById ---

        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenItExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var service = CreateService(context);

            var result = await service.GetUserById(user.id_user);

            Assert.Equal(user.id_user, result.id_user);
            Assert.Equal("a@test.com", result.email_user);
        }

        [Fact]
        public async Task GetUserById_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetUserById(999));
        }

        // --- CreateUser ---

        [Fact]
        public async Task CreateUser_ShouldPersistUser_WithHashedPassword()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = BuildCreateDto("new@test.com");

            var result = await service.CreateUser(dto);

            Assert.Equal("new@test.com", result.email_user);
            var persisted = await context.Users.FindAsync(result.id_user);
            Assert.NotNull(persisted);
            Assert.NotEqual("Password1!", persisted!.password_user);
        }

        [Fact]
        public async Task CreateUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsed()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser("dup@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = BuildCreateDto("dup@test.com");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUser(dto));
        }

        [Fact]
        public async Task CreateUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminCreatesAdminUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = BuildCreateDto("new@test.com", UserRole.Admin);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateUser(dto));
        }

        [Fact]
        public async Task CreateUser_ShouldBypassRoleVerification_WhenAvoidRoleVerificationIsTrue()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            var service = CreateService(context);
            var dto = BuildCreateDto("new@test.com", UserRole.Admin);

            var result = await service.CreateUser(dto, avoidRoleVerification: true);

            Assert.Equal(UserRole.Admin, result.role_user);
        }

        // --- UpdateUser ---

        [Fact]
        public async Task UpdateUser_ShouldUpdateProvidedFields_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("SSO");
            var service = CreateService(context);
            var dto = new UpdateUserDto { name_user = "Renamed" };

            var result = await service.UpdateUser(user.id_user, dto);

            Assert.Equal("Renamed", result.name_user);
            _jwiService.Verify(j => j.RevokeAllAccessTokenByUser(user.id_user, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsedByAnotherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user1 = BuildUser("a@test.com");
            var user2 = BuildUser("b@test.com");
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user1.id_user);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("SSO");
            var service = CreateService(context);
            var dto = new UpdateUserDto { email_user = "b@test.com" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUser(user1.id_user, dto));
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowInvalidOperationException_WhenDemotingLastAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var admin = BuildUser("admin@test.com", UserRole.Admin);
            context.Users.Add(admin);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(admin.id_user);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("SSO");
            var service = CreateService(context);
            var dto = new UpdateUserDto { role_user = UserRole.User };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateUser(admin.id_user, dto));
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminUpdatesAnotherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user + 1);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateUser(user.id_user, new UpdateUserDto { name_user = "x" }));
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("SSO");
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateUser(999, new UpdateUserDto()));
        }

        // --- DeleteUser ---

        [Fact]
        public async Task DeleteUser_ShouldRemoveUser_WhenClientIsAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user);
            var service = CreateService(context);

            await service.DeleteUser(user.id_user);

            Assert.Equal(0, await context.Users.CountAsync());
        }

        [Fact]
        public async Task DeleteUser_ShouldThrowInvalidOperationException_WhenDeletingLastAdmin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var admin = BuildUser("admin@test.com", UserRole.Admin);
            context.Users.Add(admin);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(admin.id_user);
            var service = CreateService(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteUser(admin.id_user));
            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task DeleteUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminDeletesAnotherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser("a@test.com");
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            _sessionService.Setup(s => s.GetClientId()).Returns(user.id_user + 1);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteUser(user.id_user));
        }

        [Fact]
        public async Task DeleteUser_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteUser(999));
        }
    }
}
