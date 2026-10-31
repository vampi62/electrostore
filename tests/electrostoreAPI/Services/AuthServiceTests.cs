using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;
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
            return new AuthService(_mapper, context, configuration, _kafkaProducerService.Object, _sessionService.Object, _userService.Object, _jwtService.Object, _jwiService.Object, CreateLogger<AuthService>());
        }

        private static Models.Users BuildUser(int id, string email, string password, UserRole role = UserRole.User)
        {
            return new Models.Users
            {
                id_user = id,
                name_user = "Nom",
                firstname_user = "Prenom",
                email_user = email,
                role_user = role,
                UsersPasswords = [new Models.UsersPasswords { password_user = BCrypt.Net.BCrypt.HashPassword(password) }]
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
            Assert.NotEmpty(result.state);
            Assert.StartsWith("https://provider.example.com/authorize?", result.auth_url);
            Assert.Contains("client_id=client-id", result.auth_url);
            Assert.Contains("state=", result.auth_url);
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
            Assert.StartsWith("https://provider.example.com/authorize?", result.auth_url);
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
            var request = new SsoLoginRequest { code = "some-code", state = "invalid-state-" + Guid.NewGuid() };
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
                await authService.ForgotPassword(new ForgotPasswordRequest { email = "user@test.com" });
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
            await authService.ForgotPassword(new ForgotPasswordRequest { email = "user@test.com" });
            // Assert
            var userPassword = await context.UsersPasswords.FindAsync(1);
            Assert.NotNull(userPassword!.reset_token);
            Assert.NotNull(userPassword.reset_token_expiration);
            Assert.True(userPassword.reset_token_expiration > DateTime.Now);
        }

        [Fact]
        public async Task ForgotPassword_ShouldNotSendAnotherEmail_WithinTheCooldown()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            await authService.ForgotPassword(new ForgotPasswordRequest { email = "user@test.com" });
            var firstToken = (await context.UsersPasswords.FindAsync(1))!.reset_token;
            await authService.ForgotPassword(new ForgotPasswordRequest { email = "user@test.com" });
            // Assert
            var userPassword = await context.UsersPasswords.FindAsync(1);
            Assert.NotNull(userPassword!.reset_token_sent_at);
            Assert.Equal(firstToken, userPassword.reset_token);
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "user@test.com-password-reset", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_ShouldSendNewToken_AfterTheCooldown()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser(1, "user@test.com", "Password1!");
            var oldToken = Guid.NewGuid();
            user.UsersPasswords.Single().reset_token = oldToken;
            user.UsersPasswords.Single().reset_token_sent_at = DateTime.UtcNow.AddMinutes(-5);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            await authService.ForgotPassword(new ForgotPasswordRequest { email = "user@test.com" });
            // Assert
            var userPassword = await context.UsersPasswords.FindAsync(1);
            Assert.NotEqual(oldToken, userPassword!.reset_token);
            Assert.True(userPassword.reset_token_sent_at > DateTime.UtcNow.AddMinutes(-1));
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "user@test.com-password-reset", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // --- ResendVerification / VerifyEmail ---

        [Fact]
        public async Task ResendVerification_ShouldAskTheUserServiceToSendTheEmail()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = CreateService(context, BuildEmptyConfiguration());

            await authService.ResendVerification(new ResendVerificationRequest { email = "user@test.com" });

            _userService.Verify(u => u.SendEmailVerification("user@test.com"), Times.Once);
        }

        [Fact]
        public async Task ResendVerification_ShouldThrowInvalidOperationException_WhenSmtpIsDisabled()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = CreateService(context, BuildSmtpDisabledConfiguration());

            await Assert.ThrowsAsync<InvalidOperationException>(() => authService.ResendVerification(new ResendVerificationRequest { email = "user@test.com" }));
            _userService.Verify(u => u.SendEmailVerification(It.IsAny<string>()), Times.Never);
        }

        private async Task<Guid> SeedUnverifiedUser(ApplicationDbContext context, DateTime expiration)
        {
            var token = Guid.NewGuid();
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.email_verification_token = token;
            user.email_verification_token_expiration = expiration;
            user.email_verification_token_sent_at = DateTime.UtcNow;
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return token;
        }

        [Fact]
        public async Task VerifyEmail_ShouldVerifyUserAndClearToken_WhenTokenValid()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var token = await SeedUnverifiedUser(context, DateTime.UtcNow.AddHours(1));
            var authService = CreateService(context, BuildEmptyConfiguration());

            await authService.VerifyEmail(new VerifyEmailRequest { email = "user@test.com", token = token.ToString().ToUpperInvariant() });

            var user = await context.Users.FindAsync(1);
            Assert.True(user!.is_verified);
            Assert.Null(user.email_verification_token);
            Assert.Null(user.email_verification_token_expiration);
        }

        [Fact]
        public async Task VerifyEmail_ShouldThrowInvalidOperationException_WhenTokenExpired()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var token = await SeedUnverifiedUser(context, DateTime.UtcNow.AddHours(-1));
            var authService = CreateService(context, BuildEmptyConfiguration());

            await Assert.ThrowsAsync<InvalidOperationException>(() => authService.VerifyEmail(new VerifyEmailRequest { email = "user@test.com", token = token.ToString() }));
            Assert.False((await context.Users.FindAsync(1))!.is_verified);
        }

        [Theory]
        [InlineData("not-a-guid", "user@test.com")]
        [InlineData("00000000-0000-0000-0000-000000000000", "user@test.com")]
        [InlineData(null, "other@test.com")]
        public async Task VerifyEmail_ShouldThrowInvalidOperationException_WhenTokenOrEmailDoNotMatch(string? token, string email)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var validToken = await SeedUnverifiedUser(context, DateTime.UtcNow.AddHours(1));
            var authService = CreateService(context, BuildEmptyConfiguration());

            await Assert.ThrowsAsync<InvalidOperationException>(() => authService.VerifyEmail(new VerifyEmailRequest { email = email, token = token ?? validToken.ToString() }));
            Assert.False((await context.Users.FindAsync(1))!.is_verified);
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
                await authService.ForgotPassword(new ForgotPasswordRequest { email = "missing@test.com" });
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
                await authService.ResetPassword(new ResetPasswordRequest { email = "user@test.com", token = Guid.NewGuid().ToString(), password = "NewPassword1!" });
            });
        }

        [Fact]
        public async Task ResetPassword_ShouldThrowInvalidOperationException_WhenTokenInvalid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.UsersPasswords.Single().reset_token = Guid.NewGuid();
            user.UsersPasswords.Single().reset_token_expiration = DateTime.Now.AddHours(1);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await authService.ResetPassword(new ResetPasswordRequest { email = "user@test.com", token = Guid.NewGuid().ToString(), password = "NewPassword1!" });
            });
        }

        [Fact]
        public async Task ResetPassword_ShouldThrowInvalidOperationException_WhenTokenExpired()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var resetToken = Guid.NewGuid();
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.UsersPasswords.Single().reset_token = resetToken;
            user.UsersPasswords.Single().reset_token_expiration = DateTime.Now.AddHours(-1);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await authService.ResetPassword(new ResetPasswordRequest { email = "user@test.com", token = resetToken.ToString(), password = "NewPassword1!" });
            });
        }

        [Fact]
        public async Task ResetPassword_ShouldUpdatePasswordAndRevokeTokens_WhenTokenValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var resetToken = Guid.NewGuid();
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.UsersPasswords.Single().reset_token = resetToken;
            user.UsersPasswords.Single().reset_token_expiration = DateTime.Now.AddHours(1);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var configuration = BuildEmptyConfiguration();
            var authService = CreateService(context, configuration);
            // Act
            await authService.ResetPassword(new ResetPasswordRequest { email = "user@test.com", token = resetToken.ToString(), password = "NewPassword1!" });
            // Assert
            var updatedPassword = await context.UsersPasswords.FindAsync(1);
            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword1!", updatedPassword!.password_user));
            Assert.Null(updatedPassword.reset_token);
            Assert.Null(updatedPassword.reset_token_expiration);
            Assert.NotNull(updatedPassword.password_last_changed);
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
            var result = await authService.LoginWithPassword(new LoginRequest { email = "user@test.com", password = "Password1!" });
            // Assert
            Assert.Equal("access-token", result.token);
            Assert.Equal("refresh-token", result.refresh_token);
            Assert.Equal("user@test.com", result.user.email_user);
            _jwiService.Verify(j => j.SaveToken(jwt, 1, "user_password", null, null), Times.Once);
            Assert.NotNull((await context.Users.FindAsync(1))!.last_login);
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
                await authService.LoginWithPassword(new LoginRequest { email = "missing@test.com", password = "Password1!" });
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
                await authService.LoginWithPassword(new LoginRequest { email = "user@test.com", password = "WrongPassword1!" });
            });
        }

        [Fact]
        public async Task LoginWithPassword_ShouldThrowUnauthorizedAccessException_WhenUserHasNoPassword()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(new Models.Users { id_user = 1, name_user = "Nom", firstname_user = "Prenom", email_user = "sso@test.com" });
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await authService.LoginWithPassword(new LoginRequest { email = "sso@test.com", password = "Password1!" });
            });
        }

        [Fact]
        public async Task LoginWithPassword_ShouldThrowUnauthorizedAccessException_WhenUserIsInactive()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.is_active = false;
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await authService.LoginWithPassword(new LoginRequest { email = "user@test.com", password = "Password1!" });
            });
            _jwiService.Verify(j => j.SaveToken(It.IsAny<Jwt>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task CheckUserPassword_ShouldReturnFalse_WhenUserHasNoPassword()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(new Models.Users { id_user = 1, name_user = "Nom", firstname_user = "Prenom", email_user = "sso@test.com" });
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act & Assert
            Assert.False(await authService.CheckUserPasswordByEmail("sso@test.com", "Password1!"));
            Assert.False(await authService.CheckUserPasswordById(1, "Password1!"));
        }

        private static Models.Users BuildExternalLoginUser() =>
            new() { id_user = 1, name_user = "Nom", firstname_user = "Prenom", email_user = "sso@test.com" };

        [Fact]
        public async Task ForgotPassword_ShouldCreateAnEmptyPassword_WhenUserHasNoPassword()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildExternalLoginUser());
            await context.SaveChangesAsync();
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            await authService.ForgotPassword(new ForgotPasswordRequest { email = "sso@test.com" });
            // Assert
            var userPassword = await context.UsersPasswords.SingleAsync();
            Assert.Equal(1, userPassword.id_user);
            Assert.Equal(string.Empty, userPassword.password_user);
            Assert.NotNull(userPassword.reset_token);
            Assert.NotNull(userPassword.reset_token_expiration);
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "sso@test.com-password-reset", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_ShouldNotCreateAPassword_WhenUserDoesNotExist()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act
            await authService.ForgotPassword(new ForgotPasswordRequest { email = "missing@test.com" });
            // Assert
            Assert.Empty(await context.UsersPasswords.ToListAsync());
            _kafkaProducerService.Verify(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ExternalLoginUser_ShouldNotBeAbleToLogInWithPassword_UntilTheResetIsDone()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildExternalLoginUser());
            await context.SaveChangesAsync();
            _jwtService.Setup(j => j.GenerateToken(It.IsAny<ReadUserDto>(), "user_password")).ReturnsAsync(BuildJwt());
            var authService = CreateService(context, BuildEmptyConfiguration());
            await authService.ForgotPassword(new ForgotPasswordRequest { email = "sso@test.com" });
            // Act & Assert: the empty password matches nothing, even an empty one
            foreach (var attempt in new[] { string.Empty, " ", "Password1!" })
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginWithPassword(new LoginRequest { email = "sso@test.com", password = attempt }));
                Assert.False(await authService.CheckUserPasswordByEmail("sso@test.com", attempt));
                Assert.False(await authService.CheckUserPasswordById(1, attempt));
            }
            // the reset gives him a real password
            var resetToken = (await context.UsersPasswords.SingleAsync()).reset_token!.Value;
            await authService.ResetPassword(new ResetPasswordRequest { email = "sso@test.com", token = resetToken.ToString(), password = "NewPassword1!" });
            var result = await authService.LoginWithPassword(new LoginRequest { email = "sso@test.com", password = "NewPassword1!" });
            Assert.Equal("sso-access", result.token);
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
            _jwiService.Verify(j => j.SaveToken(newJwt, 1, "user_password", sessionId, null), Times.Once);
            Assert.NotNull((await context.Users.FindAsync(1))!.last_login);
        }

        [Fact]
        public async Task RefreshJwt_ShouldThrowUnauthorizedAccessException_WhenUserIsInactive()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser(1, "user@test.com", "Password1!");
            user.is_active = false;
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            _sessionService.Setup(s => s.GetTokenId()).Returns("old-token-id");
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("user_password");
            _jwiService.Setup(j => j.GetSessionIdByTokenId("old-token-id", 1)).ReturnsAsync(Guid.NewGuid());
            var authService = CreateService(context, BuildEmptyConfiguration());
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.RefreshJwt());
            _jwiService.Verify(j => j.SaveToken(It.IsAny<Jwt>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<int?>()), Times.Never);
            Assert.Null((await context.Users.FindAsync(1))!.last_login);
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

        // --- LoginWithSSO ---

        private static Jwt BuildJwt() => new()
        {
            token = "sso-access",
            refresh_token = "sso-refresh",
            expire_date_token = DateTime.UtcNow.AddMinutes(15),
            expire_date_refresh_token = DateTime.UtcNow.AddDays(7)
        };

        private static IConfiguration BuildSsoConfiguration(FakeOidcServer server, Dictionary<string, string?>? extra = null)
        {
            var values = new Dictionary<string, string?>
            {
                { "OAuth:Authentik:ClientId", "client-id" },
                { "OAuth:Authentik:ClientSecret", "client-secret" },
                { "OAuth:Authentik:Authority", server.Authority },
                { "OAuth:Authentik:RedirectUri", "https://app.example.com/callback" },
                { "OAuth:Authentik:Scope", "openid email" },
                { "OAuth:Authentik:GroupMapping:Admin", "admins" },
                { "OAuth:Authentik:GroupMapping:Moderator", "moderators" }
            };
            if (extra is not null)
            {
                foreach (var kv in extra)
                {
                    values[kv.Key] = kv.Value;
                }
            }
            return BuildConfiguration(values);
        }

        private async Task<(AuthService service, string state)> PrepareSsoLogin(ApplicationDbContext context, FakeOidcServer server, IConfiguration? configuration = null)
        {
            var service = CreateService(context, configuration ?? BuildSsoConfiguration(server));
            var state = (await service.GetSSOAuthUrl("authentik")).state;
            _jwtService.Setup(j => j.GenerateToken(It.IsAny<ReadUserDto>(), "sso_authentik")).ReturnsAsync(BuildJwt());
            return (service, state);
        }

        private void SetupCreateExternalUser(int idUser, int externalLoginId)
        {
            _userService
                .Setup(u => u.CreateExternalUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string name, string firstname, string email, UserRole role, string provider, string subject) => (new ReadUserDto
                {
                    id_user = idUser,
                    name_user = name,
                    firstname_user = firstname,
                    email_user = email,
                    role_user = role
                }, externalLoginId));
        }

        [Fact]
        public async Task LoginWithSSO_ShouldCreateExternalUserWithMappedRole_AndReturnTokens()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            using var server = new FakeOidcServer
            {
                UserInfoBody = """{"sub":"sub-42","email":"new@example.com","given_name":"Given","family_name":"Family","groups":["moderators","admins"]}"""
            };
            var (service, state) = await PrepareSsoLogin(context, server);
            SetupCreateExternalUser(42, 7);

            var result = await service.LoginWithSSO("authentik", new SsoLoginRequest { code = "the-code", state = state });

            Assert.Equal("sso-access", result.token);
            Assert.Equal("sso-refresh", result.refresh_token);
            Assert.Equal(42, result.user.id_user);
            _userService.Verify(u => u.CreateExternalUser("Family", "Given", "new@example.com", UserRole.Admin, "authentik", "sub-42"), Times.Once);
            _jwiService.Verify(j => j.SaveToken(It.IsAny<Jwt>(), 42, "sso_authentik", null, 7), Times.Once);
            Assert.Contains("code=the-code", server.LastTokenRequestBody);
            Assert.Equal("Bearer access", server.LastUserInfoAuthorization);
            _kafkaProducerService.Verify(k => k.PublishAsync("notification-requests", "new@example.com-sso-login", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldUseDefaultNamesAndUserRole_WhenProviderOmitsThem()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            using var server = new FakeOidcServer
            {
                UserInfoBody = """{"sub":"1","email":"anon@example.com","groups":[]}"""
            };
            var (service, state) = await PrepareSsoLogin(context, server);
            SetupCreateExternalUser(1, 1);

            await service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state });

            _userService.Verify(u => u.CreateExternalUser("SSO", "User", "anon@example.com", UserRole.User, "authentik", "1"), Times.Once);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldThrowInvalidOperationException_WhenProviderOmitsSubject()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            using var server = new FakeOidcServer
            {
                UserInfoBody = """{"email":"nosub@example.com","groups":[]}"""
            };
            var (service, state) = await PrepareSsoLogin(context, server);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state }));
            _userService.Verify(u => u.CreateExternalUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldReuseExistingUser_AndUpdateRoleFromGroups()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "existing@example.com", "Password1!"));
            await context.SaveChangesAsync();
            using var server = new FakeOidcServer
            {
                UserInfoBody = """{"sub":"1","email":"existing@example.com","groups":["moderators"]}"""
            };
            var (service, state) = await PrepareSsoLogin(context, server);

            var result = await service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state });

            Assert.Equal(1, result.user.id_user);
            Assert.Equal(UserRole.Moderator, result.user.role_user);
            Assert.Equal(UserRole.Moderator, (await context.Users.FindAsync(1))!.role_user);
            _userService.Verify(u => u.CreateExternalUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldLinkExistingUserFoundByEmailToTheExternalLogin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "existing@example.com", "Password1!"));
            await context.SaveChangesAsync();
            using var server = new FakeOidcServer
            {
                UserInfoBody = """{"sub":"sub-1","email":"existing@example.com","groups":[]}"""
            };
            var (service, state) = await PrepareSsoLogin(context, server);

            await service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state });

            var externalLogin = await context.UsersExternalLogins.SingleAsync();
            Assert.Equal(1, externalLogin.id_user);
            Assert.Equal("authentik", externalLogin.provider);
            Assert.Equal("sub-1", externalLogin.provider_subject);
            _jwiService.Verify(j => j.SaveToken(It.IsAny<Jwt>(), 1, "sso_authentik", null, externalLogin.id_user_external_login), Times.Once);
            Assert.NotNull((await context.Users.FindAsync(1))!.last_login);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldFindUserByExternalLogin_EvenWhenTheEmailChanged()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "old@example.com", "Password1!"));
            var existingLogin = new Models.UsersExternalLogins { id_user = 1, provider = "authentik", provider_subject = "sub-1" };
            context.UsersExternalLogins.Add(existingLogin);
            await context.SaveChangesAsync();
            using var server = new FakeOidcServer
            {
                UserInfoBody = """{"sub":"sub-1","email":"new@example.com","groups":[]}"""
            };
            var (service, state) = await PrepareSsoLogin(context, server);

            var result = await service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state });

            Assert.Equal(1, result.user.id_user);
            Assert.Single(await context.UsersExternalLogins.ToListAsync());
            _jwiService.Verify(j => j.SaveToken(It.IsAny<Jwt>(), 1, "sso_authentik", null, existingLogin.id_user_external_login), Times.Once);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldRejectInactiveUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser(1, "existing@example.com", "Password1!");
            user.is_active = false;
            context.Users.Add(user);
            await context.SaveChangesAsync();
            using var server = new FakeOidcServer
            {
                UserInfoBody = """{"sub":"sub-1","email":"existing@example.com","groups":[]}"""
            };
            var (service, state) = await PrepareSsoLogin(context, server);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state }));
            _jwiService.Verify(j => j.SaveToken(It.IsAny<Jwt>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<int?>()), Times.Never);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldKeepExistingUserUntouched_WhenRoleAlreadyMatches()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "existing@example.com", "Password1!"));
            await context.SaveChangesAsync();
            using var server = new FakeOidcServer
            {
                UserInfoBody = """{"sub":"1","email":"existing@example.com","groups":[]}"""
            };
            var (service, state) = await PrepareSsoLogin(context, server);

            var result = await service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state });

            Assert.Equal(UserRole.User, result.user.role_user);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldStillSucceed_WhenNotificationCannotBePublished()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "sso@example.com", "Password1!"));
            await context.SaveChangesAsync();
            using var server = new FakeOidcServer();
            var (service, state) = await PrepareSsoLogin(context, server);
            _kafkaProducerService
                .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka down"));

            var result = await service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state });

            Assert.Equal("sso-access", result.token);
        }

        [Fact]
        public async Task LoginWithSSO_ShouldRejectReusedState()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "sso@example.com", "Password1!"));
            await context.SaveChangesAsync();
            using var server = new FakeOidcServer();
            var (service, state) = await PrepareSsoLogin(context, server);
            await service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state }));
        }

        [Fact]
        public async Task LoginWithSSO_ShouldThrowArgumentException_WhenConfigurationIsIncomplete()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            using var server = new FakeOidcServer();
            var configuration = BuildSsoConfiguration(server, new Dictionary<string, string?> { { "OAuth:Authentik:ClientSecret", null } });
            var (service, state) = await PrepareSsoLogin(context, server, configuration);

            await Assert.ThrowsAsync<ArgumentException>(() => service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state }));
        }

        [Fact]
        public async Task LoginWithSSO_ShouldThrowHttpRequestException_WhenTokenExchangeFails()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            using var server = new FakeOidcServer { TokenStatusCode = 400, TokenBody = """{"error":"invalid_grant"}""" };
            var (service, state) = await PrepareSsoLogin(context, server);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.LoginWithSSO("authentik", new SsoLoginRequest { code = "bad", state = state }));
        }

        [Fact]
        public async Task LoginWithSSO_ShouldThrowInvalidOperationException_WhenTokenResponseIsNull()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            using var server = new FakeOidcServer { TokenBody = "null" };
            var (service, state) = await PrepareSsoLogin(context, server);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state }));
        }

        [Fact]
        public async Task LoginWithSSO_ShouldThrowHttpRequestException_WhenUserInfoRequestFails()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            using var server = new FakeOidcServer { UserInfoStatusCode = 401, UserInfoBody = "unauthorized" };
            var (service, state) = await PrepareSsoLogin(context, server);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state }));
        }

        [Fact]
        public async Task LoginWithSSO_ShouldThrowInvalidOperationException_WhenUserInfoResponseIsNull()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            using var server = new FakeOidcServer { UserInfoBody = "null" };
            var (service, state) = await PrepareSsoLogin(context, server);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.LoginWithSSO("authentik", new SsoLoginRequest { code = "c", state = state }));
        }

        // --- notification failures ---

        [Fact]
        public async Task ForgotPassword_ShouldStillSetResetToken_WhenNotificationCannotBePublished()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            _kafkaProducerService
                .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka down"));
            var service = CreateService(context, BuildEmptyConfiguration());

            await service.ForgotPassword(new ForgotPasswordRequest { email = "user@test.com" });

            Assert.NotNull((await context.UsersPasswords.FindAsync(1))!.reset_token);
        }

        [Fact]
        public async Task ResetPassword_ShouldStillSucceed_WhenNotificationCannotBePublished()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = BuildUser(1, "user@test.com", "OldPassword1!");
            user.UsersPasswords.Single().reset_token = Guid.NewGuid();
            user.UsersPasswords.Single().reset_token_expiration = DateTime.Now.AddHours(1);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            _kafkaProducerService
                .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka down"));
            var service = CreateService(context, BuildEmptyConfiguration());

            await service.ResetPassword(new ResetPasswordRequest { email = "user@test.com", token = user.UsersPasswords.Single().reset_token.ToString()!, password = "NewPassword1!" });

            Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword1!", (await context.UsersPasswords.FindAsync(1))!.password_user));
        }

        [Fact]
        public async Task LoginWithPassword_ShouldStillSucceed_WhenNotificationCannotBePublished()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1, "user@test.com", "Password1!"));
            await context.SaveChangesAsync();
            _jwtService.Setup(j => j.GenerateToken(It.IsAny<ReadUserDto>(), "user_password")).ReturnsAsync(BuildJwt());
            _kafkaProducerService
                .Setup(k => k.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("kafka down"));
            var service = CreateService(context, BuildEmptyConfiguration());

            var result = await service.LoginWithPassword(new LoginRequest { email = "user@test.com", password = "Password1!" });

            Assert.Equal("sso-access", result.token);
        }

        [Fact]
        public async Task GetSSOAuthUrl_ShouldDiscardExpiredStatesWithoutFailing()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context, BuildOAuthConfiguration("Authentik"));

            var first = await service.GetSSOAuthUrl("authentik");
            var second = await service.GetSSOAuthUrl("authentik");

            Assert.NotEqual(first.state, second.state);
        }
    }
}
