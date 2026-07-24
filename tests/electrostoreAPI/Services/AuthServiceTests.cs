using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Services.AuthService;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.JwtService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class AuthServiceTests : TestBase
    {
        private readonly Mock<IKafkaProducerService> _kafkaProducerService;
        private readonly Mock<ISessionService> _sessionService;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<IJwtService> _jwtService;
        private readonly Mock<IJwiService> _jwiService;

        public AuthServiceTests()
        {
            _kafkaProducerService = new Mock<IKafkaProducerService>();
            _sessionService = new Mock<ISessionService>();
            _userService = new Mock<IUserService>();
            _jwtService = new Mock<IJwtService>();
            _jwiService = new Mock<IJwiService>();
        }

        private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static IConfiguration BuildEmptyConfiguration()
        {
            return BuildConfiguration(new Dictionary<string, string?>());
        }

        private static IConfiguration BuildSmtpDisabledConfiguration()
        {
            return BuildConfiguration(new Dictionary<string, string?> { { "SMTP:Enable", "false" } });
        }

        private static IConfiguration BuildOAuthConfiguration(
            string providerKey,
            string clientId = "client-id",
            string authority = "https://provider.example.com/authorize",
            string redirectUri = "https://app.example.com/callback",
            string scope = "openid email")
        {
            return BuildConfiguration(new Dictionary<string, string?>
            {
                { $"OAuth:{providerKey}:ClientId", clientId },
                { $"OAuth:{providerKey}:Authority", authority },
                { $"OAuth:{providerKey}:RedirectUri", redirectUri },
                { $"OAuth:{providerKey}:Scope", scope }
            });
        }

        private AuthService CreateService(ApplicationDbContext context, IConfiguration configuration)
        {
            return new AuthService(_mapper, context, configuration, _kafkaProducerService.Object, _sessionService.Object, _userService.Object, _jwtService.Object, _jwiService.Object);
        }

        private static Models.Users BuildUser(int id, string email, string password, UserRole role = UserRole.User)
        {
            return new Models.Users
            {
                id_user = id,
                nom_user = "Nom",
                prenom_user = "Prenom",
                email_user = email,
                mdp_user = BCrypt.Net.BCrypt.HashPassword(password),
                role_user = role
            };
        }

        // --- GetSSOAuthUrl ---

        [Fact]
        public async Task GetSSOAuthUrl_ShouldReturnAuthUrl_WhenConfigValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var configuration = BuildOAuthConfiguration("Google");
            var authService = CreateService(context, configuration);
            // Act
            var result = await authService.GetSSOAuthUrl("google");
            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.State);
            Assert.StartsWith("https://provider.example.com/authorize?", result.AuthUrl);
            Assert.Contains("client_id=client-id", result.AuthUrl);
            Assert.Contains("state=", result.AuthUrl);
        }

        [Fact]
        public async Task GetSSOAuthUrl_ShouldHandlePascalCaseConversion_ForSsoMethodWithUnderscore()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var configuration = BuildOAuthConfiguration("GoogleWorkspace");
            var authService = CreateService(context, configuration);
            // Act
            var result = await authService.GetSSOAuthUrl("google_workspace");
            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("https://provider.example.com/authorize?", result.AuthUrl);
        }

        [Fact]
        public async Task GetSSOAuthUrl_ShouldThrowArgumentException_WhenConfigMissing()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await authService.GetSSOAuthUrl("unknown_provider");
            });
        }

        // --- LoginWithSSO (state validation only, other branches need real HTTP) ---

        [Fact]
        public async Task LoginWithSSO_ShouldThrowUnauthorizedAccessException_WhenStateIsInvalid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            var request = new SsoLoginRequest { Code = "some-code", State = "invalid-state-" + Guid.NewGuid() };
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await authService.LoginWithSSO("google", request);
            });
        }

        // --- CheckUserPasswordByEmail ---

        [Fact]
        public async Task CheckUserPasswordByEmail_ShouldReturnTrue_ForCorrectPassword()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            var result = await authService.CheckUserPasswordByEmail("user@test.com", "Password1!");
            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckUserPasswordByEmail_ShouldReturnFalse_ForIncorrectPassword()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            var result = await authService.CheckUserPasswordByEmail("user@test.com", "WrongPassword1!");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckUserPasswordByEmail_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await authService.CheckUserPasswordByEmail("missing@test.com", "Password1!");
            });
        }

        // --- CheckUserPasswordById ---

        [Fact]
        public async Task CheckUserPasswordById_ShouldReturnTrue_ForCorrectPassword()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            var result = await authService.CheckUserPasswordById(1, "Password1!");
            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckUserPasswordById_ShouldReturnFalse_ForIncorrectPassword()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            var result = await authService.CheckUserPasswordById(1, "WrongPassword1!");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckUserPasswordById_ShouldReturnFalse_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            var result = await authService.CheckUserPasswordById(999, "Password1!");
            // Assert
            Assert.False(result);
        }

        // --- ForgotPassword ---

        [Fact]
        public async Task ForgotPassword_ShouldThrowInvalidOperationException_WhenSmtpExplicitlyDisabled()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var configuration = BuildSmtpDisabledConfiguration();
            var authService = CreateService(context, configuration);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await authService.ForgotPassword(new ForgotPasswordRequest { Email = "user@test.com" });
            });
        }

        [Fact]
        public async Task ForgotPassword_ShouldSetResetToken_WhenUserExistsAndSmtpNotDisabled()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act
            await authService.ForgotPassword(new ForgotPasswordRequest { Email = "user@test.com" });
            // Assert
            var user = await context.Users.FindAsync(1);
            Assert.NotNull(user!.reset_token);
            Assert.NotNull(user.reset_token_expiration);
            Assert.True(user.reset_token_expiration > DateTime.Now);
        }

        [Fact]
        public async Task ForgotPassword_ShouldNotThrow_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                await authService.ForgotPassword(new ForgotPasswordRequest { Email = "missing@test.com" });
            });
            // Assert
            Assert.Null(exception);
        }

        // --- ResetPassword ---

        [Fact]
        public async Task ResetPassword_ShouldThrowInvalidOperationException_WhenSmtpExplicitlyDisabled()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var configuration = BuildSmtpDisabledConfiguration();
            var authService = CreateService(context, configuration);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await authService.ResetPassword(new ResetPasswordRequest { Email = "user@test.com", Token = Guid.NewGuid().ToString(), Password = "NewPassword1!" });
            });
        }

        [Fact]
        public async Task ResetPassword_ShouldThrowInvalidOperationException_WhenTokenInvalid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.reset_token = Guid.NewGuid();
            user.reset_token_expiration = DateTime.Now.AddHours(1);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await authService.ResetPassword(new ResetPasswordRequest { Email = "user@test.com", Token = Guid.NewGuid().ToString(), Password = "NewPassword1!" });
            });
        }

        [Fact]
        public async Task ResetPassword_ShouldThrowInvalidOperationException_WhenTokenExpired()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var resetToken = Guid.NewGuid();
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.reset_token = resetToken;
            user.reset_token_expiration = DateTime.Now.AddHours(-1);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await authService.ResetPassword(new ResetPasswordRequest { Email = "user@test.com", Token = resetToken.ToString(), Password = "NewPassword1!" });
            });
        }

        [Fact]
        public async Task ResetPassword_ShouldUpdatePasswordAndRevokeTokens_WhenTokenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var resetToken = Guid.NewGuid();
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.reset_token = resetToken;
            user.reset_token_expiration = DateTime.Now.AddHours(1);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act
            await authService.ResetPassword(new ResetPasswordRequest { Email = "user@test.com", Token = resetToken.ToString(), Password = "NewPassword1!" });
            // Assert
            var updatedUser = await context.Users.FindAsync(1);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword1!", updatedUser!.mdp_user));
            Assert.Null(updatedUser.reset_token);
            Assert.Null(updatedUser.reset_token_expiration);
            _jwiService.Verify(j => j.RevokeAllAccessTokenByUser(1, "User reset password"), Times.Once);
            _jwiService.Verify(j => j.RevokeAllRefreshTokenByUser(1, "User reset password"), Times.Once);
        }

        // --- LoginWithPassword ---

        [Fact]
        public async Task LoginWithPassword_ShouldReturnLoginResponse_WhenCredentialsValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var jwt = new Jwt
            {
                token = "access-token",
                refresh_token = "refresh-token",
                expire_date_token = DateTime.UtcNow.AddMinutes(15),
                expire_date_refresh_token = DateTime.UtcNow.AddDays(7)
            };
            _jwtService.Setup(j => j.GenerateToken(It.IsAny<ReadUserDto>(), "user_password")).ReturnsAsync(jwt);
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            var result = await authService.LoginWithPassword(new LoginRequest { Email = "user@test.com", Password = "Password1!" });
            // Assert
            Assert.Equal("access-token", result.token);
            Assert.Equal("refresh-token", result.refresh_token);
            Assert.Equal("user@test.com", result.user.email_user);
            _jwiService.Verify(j => j.SaveToken(jwt, 1, "user_password", null), Times.Once);
        }

        [Fact]
        public async Task LoginWithPassword_ShouldThrowUnauthorizedAccessException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await authService.LoginWithPassword(new LoginRequest { Email = "missing@test.com", Password = "Password1!" });
            });
        }

        [Fact]
        public async Task LoginWithPassword_ShouldThrowUnauthorizedAccessException_WhenPasswordIncorrect()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await authService.LoginWithPassword(new LoginRequest { Email = "user@test.com", Password = "WrongPassword1!" });
            });
        }

        // --- RefreshJwt ---

        [Fact]
        public async Task RefreshJwt_ShouldReturnNewLoginResponse_AndRevokeOldPairToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var sessionId = Guid.NewGuid();
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            _sessionService.Setup(s => s.GetTokenId()).Returns("old-token-id");
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("user_password");
            _jwiService.Setup(j => j.GetSessionIdByTokenId("old-token-id", 1)).ReturnsAsync(sessionId);
            var newJwt = new Jwt
            {
                token = "new-access-token",
                refresh_token = "new-refresh-token",
                expire_date_token = DateTime.UtcNow.AddMinutes(15),
                expire_date_refresh_token = DateTime.UtcNow.AddDays(7)
            };
            _jwtService.Setup(j => j.GenerateToken(It.IsAny<ReadUserDto>(), "user_password")).ReturnsAsync(newJwt);
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            var result = await authService.RefreshJwt();
            // Assert
            Assert.Equal("new-access-token", result.token);
            Assert.Equal("new-refresh-token", result.refresh_token);
            _jwiService.Verify(j => j.RevokePairTokenByRefreshToken("old-token-id", "User refresh token", 1), Times.Once);
            _jwiService.Verify(j => j.SaveToken(newJwt, 1, "user_password", sessionId), Times.Once);
        }

        [Fact]
        public async Task RefreshJwt_ShouldThrowKeyNotFoundException_WhenUserNotFound()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            _sessionService.Setup(s => s.GetClientId()).Returns(999);
            _sessionService.Setup(s => s.GetTokenId()).Returns("old-token-id");
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("user_password");
            _jwiService.Setup(j => j.GetSessionIdByTokenId("old-token-id", 999)).ReturnsAsync(Guid.NewGuid());
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await authService.RefreshJwt();
            });
        }
    }
}
