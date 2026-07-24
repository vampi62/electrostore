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
        private readonly Mock<IKafkaProducerService> _kafkaProducerService;
        private readonly Mock<ISessionService> _sessionService;
        private readonly Mock<IJwiService> _jwiService;

        public UserServiceTests()
        {
            _kafkaProducerService = new Mock<IKafkaProducerService>();
            _sessionService = new Mock<ISessionService>();
            _jwiService = new Mock<IJwiService>();
        }

        private UserService CreateService(ApplicationDbContext context)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
            return new UserService(_mapper, context, configuration, _kafkaProducerService.Object, _sessionService.Object, _jwiService.Object);
        }

        private void SetClient(int id, UserRole role)
        {
            _sessionService.Setup(s => s.GetClientId()).Returns(id);
            _sessionService.Setup(s => s.GetClientRole()).Returns(role);
        }

        private static Users BuildUser(int id, string email, UserRole role = UserRole.User, string password = "Password1!")
        {
            return new Users
            {
                id_user = id,
                nom_user = "Nom",
                prenom_user = "Prenom",
                email_user = email,
                mdp_user = BCrypt.Net.BCrypt.HashPassword(password),
                role_user = role
            };
        }

        // --- GetUsers ---

        [Fact]
        public async Task GetUsers_ShouldReturnAll_Paginated()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            context.Users.Add(BuildUser(2, "b@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetUsers();
            // Assert
            Assert.Equal(2, result.data.Count());
            Assert.Equal(2, result.pagination.total);
        }

        [Fact]
        public async Task GetUsers_ShouldFilterByIdResearch()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            context.Users.Add(BuildUser(2, "b@test.com"));
            context.Users.Add(BuildUser(3, "c@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetUsers(idResearch: new List<int> { 2 });
            // Assert
            var user = Assert.Single(result.data);
            Assert.Equal(2, user.id_user);
        }

        // --- GetUserById ---

        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetUserById(1);
            // Assert
            Assert.Equal("a@test.com", result.email_user);
        }

        [Fact]
        public async Task GetUserById_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.GetUserById(999);
            });
        }

        // --- GetUserByIdAsync ---

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            // Act
            var result = await service.GetUserByIdAsync(1);
            // Assert
            Assert.NotNull(result);
            Assert.Equal("a@test.com", result!.email_user);
        }

        [Fact]
        public async Task GetUserByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            // Act
            var result = await service.GetUserByIdAsync(999);
            // Assert
            Assert.Null(result);
        }

        // --- CreateUser ---

        [Fact]
        public async Task CreateUser_ShouldThrowUnauthorizedAccessException_WhenNonAdminCreatesElevatedRole()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            var dto = new CreateUserDto { nom_user = "N", prenom_user = "P", email_user = "new@test.com", mdp_user = "Password1!", role_user = UserRole.Admin };
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.CreateUser(dto);
            });
        }

        [Fact]
        public async Task CreateUser_ShouldAllowNonAdminToCreateUserRole()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            var dto = new CreateUserDto { nom_user = "N", prenom_user = "P", email_user = "new@test.com", mdp_user = "Password1!", role_user = UserRole.User };
            // Act
            var result = await service.CreateUser(dto);
            // Assert
            Assert.Equal("new@test.com", result.email_user);
        }

        [Fact]
        public async Task CreateUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsed()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "existing@test.com"));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateUserDto { nom_user = "N", prenom_user = "P", email_user = "existing@test.com", mdp_user = "Password1!", role_user = UserRole.User };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateUser(dto);
            });
        }

        [Fact]
        public async Task CreateUser_ShouldCreateUserAndPublishNotification_WhenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClient(1, UserRole.Admin);
            var service = CreateService(context);
            var dto = new CreateUserDto { nom_user = "N", prenom_user = "P", email_user = "new@test.com", mdp_user = "Password1!", role_user = UserRole.Admin };
            // Act
            var result = await service.CreateUser(dto);
            // Assert
            Assert.Equal("new@test.com", result.email_user);
            Assert.Equal(1, await context.Users.CountAsync());
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "new@test.com-account-created", It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task CreateUser_ShouldSkipRoleCheck_WhenAvoidRoleVerificationTrue()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateUserDto { nom_user = "N", prenom_user = "P", email_user = "new@test.com", mdp_user = "Password1!", role_user = UserRole.Admin };
            // Act
            var result = await service.CreateUser(dto, avoidRoleVerification: true);
            // Assert
            Assert.Equal(UserRole.Admin, result.role_user);
            _sessionService.Verify(s => s.GetClientRole(), Times.Never);
        }

        // --- CreateFirstAdminUser ---

        [Fact]
        public async Task CreateFirstAdminUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsed()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "existing@test.com"));
            await context.SaveChangesAsync();
            var service = CreateService(context);
            var dto = new CreateUserDto { nom_user = "N", prenom_user = "P", email_user = "existing@test.com", mdp_user = "Password1!", role_user = UserRole.Admin };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.CreateFirstAdminUser(dto);
            });
        }

        [Fact]
        public async Task CreateFirstAdminUser_ShouldCreateUser_WithoutRoleCheck()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var dto = new CreateUserDto { nom_user = "N", prenom_user = "P", email_user = "admin@test.com", mdp_user = "Password1!", role_user = UserRole.Admin };
            // Act
            var result = await service.CreateFirstAdminUser(dto);
            // Assert
            Assert.Equal(UserRole.Admin, result.role_user);
            _sessionService.Verify(s => s.GetClientRole(), Times.Never);
        }

        // --- UpdateUser ---

        [Fact]
        public async Task UpdateUser_ShouldThrowUnauthorizedAccessException_WhenNotSelfNorAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.UpdateUser(1, new UpdateUserDto { nom_user = "New" });
            });
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClient(1, UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.UpdateUser(999, new UpdateUserDto());
            });
        }

        [Fact]
        public async Task UpdateUser_ShouldAllowSelfUpdate()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            // Act
            var result = await service.UpdateUser(1, new UpdateUserDto { nom_user = "New name" });
            // Assert
            Assert.Equal("New name", result.nom_user);
            _jwiService.Verify(j => j.RevokeAllAccessTokenByUser(1, "User update account"), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowInvalidOperationException_WhenEmailAlreadyUsedByAnotherUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            context.Users.Add(BuildUser(2, "b@test.com"));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.UpdateUser(1, new UpdateUserDto { email_user = "b@test.com" });
            });
        }

        [Fact]
        public async Task UpdateUser_ShouldThrowInvalidOperationException_WhenDemotingLastAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "admin@test.com", UserRole.Admin));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.UpdateUser(1, new UpdateUserDto { role_user = UserRole.User });
            });
        }

        [Fact]
        public async Task UpdateUser_ShouldUpdatePasswordAndRevokeTokens()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            // Act
            await service.UpdateUser(1, new UpdateUserDto { mdp_user = "NewPassword1!" });
            // Assert
            var updated = await context.Users.FindAsync(1);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword1!", updated!.mdp_user));
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "a@test.com-password-changed", It.IsAny<string>(), default), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_ShouldNotifyBothEmails_WhenEmailChanged()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "old@test.com"));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            // Act
            await service.UpdateUser(1, new UpdateUserDto { email_user = "new@test.com" });
            // Assert
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "new@test.com-email-changed", It.IsAny<string>(), default), Times.Once);
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "old@test.com-email-changed", It.IsAny<string>(), default), Times.Once);
        }

        // --- DeleteUser ---

        [Fact]
        public async Task DeleteUser_ShouldThrowUnauthorizedAccessException_WhenNotSelfNorAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            await context.SaveChangesAsync();
            SetClient(2, UserRole.User);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await service.DeleteUser(1);
            });
        }

        [Fact]
        public async Task DeleteUser_ShouldThrowKeyNotFoundException_WhenNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetClient(1, UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await service.DeleteUser(999);
            });
        }

        [Fact]
        public async Task DeleteUser_ShouldThrowInvalidOperationException_WhenDeletingLastAdmin()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "admin@test.com", UserRole.Admin));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.Admin);
            var service = CreateService(context);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await service.DeleteUser(1);
            });
        }

        [Fact]
        public async Task DeleteUser_ShouldDeleteUserAndRevokeTokensAndNotify()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "a@test.com"));
            await context.SaveChangesAsync();
            SetClient(1, UserRole.User);
            var service = CreateService(context);
            // Act
            await service.DeleteUser(1);
            // Assert
            Assert.Equal(0, await context.Users.CountAsync());
            _jwiService.Verify(j => j.RevokeAllAccessTokenByUser(1, "User delete account"), Times.Once);
            _jwiService.Verify(j => j.RevokeAllRefreshTokenByUser(1, "User delete account"), Times.Once);
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "a@test.com-account-deleted", It.IsAny<string>(), default), Times.Once);
        }
    }
}
