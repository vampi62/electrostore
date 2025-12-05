using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using AutoMapper;
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.AuthService;
using electrostore.Services.JwtService;
using electrostore.Services.JwiService;
using electrostore.Services.SessionService;
using electrostore.Services.SmtpService;
using electrostore.Services.UserService;

using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class AuthServiceTests : TestBase
    {
        private readonly Mock<ISmtpService> _smtpService;
        private readonly Mock<IJwiService> _jwiService;
        private readonly Mock<ISessionService> _sessionService;
        private readonly Mock<IUserService> _userService;
        private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _configuration;
        private readonly Mock<IJwtService> _jwtService;
        
        public AuthServiceTests()
        {
            _smtpService = new Mock<ISmtpService>();
            _smtpService.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _jwiService = new Mock<IJwiService>();
            _jwiService.Setup(j => j.RevokeAllAccessTokenByUser(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _jwiService.Setup(j => j.RevokeAllRefreshTokenByUser(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _jwiService.Setup(j => j.SaveToken(It.IsAny<Jwt>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
            _jwiService.Setup(j => j.RevokePairTokenByRefreshToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _sessionService = new Mock<ISessionService>();
            _userService = new Mock<IUserService>();
            _configuration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _jwtService = new Mock<IJwtService>();
            _jwtService.Setup(j => j.GenerateToken(It.IsAny<ReadUserDto>(), It.IsAny<string>()))
                .ReturnsAsync(new Jwt
                {
                    token = "test-token",
                    expire_date_token = DateTime.Now.AddMinutes(15),
                    refresh_token = "test-refresh-token",
                    expire_date_refresh_token = DateTime.Now.AddDays(7),
                    token_id = Guid.NewGuid(),
                    refresh_token_id = Guid.NewGuid(),
                    created_at = DateTime.Now
                });
        }

        [Fact]
        public async Task CheckUserPasswordByEmail_ShouldReturnTrue_WhenPasswordIsCorrect()
        {
            // Arrange
            var testEmail = "test@test.com";
            var testPassword = "Test@1234";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(testPassword);
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = testEmail,
                mdp_user = hashedPassword
            };
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act
            var result = await authService.CheckUserPasswordByEmail(testEmail, testPassword);
            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CheckUserPasswordByEmail_ShouldReturnFalse_WhenPasswordIsIncorrect()
        {
            // Arrange
            var testEmail = "test@test.com";
            var testPassword = "Test@1234";
            var wrongPassword = "WrongPassword";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(testPassword);
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = testEmail,
                mdp_user = hashedPassword
            };
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act
            var result = await authService.CheckUserPasswordByEmail(testEmail, wrongPassword);
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckUserPasswordById_ShouldReturnFalse_WhenUserDoesNotExistOrPasswordIsIncorrect()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act
            var result = await authService.CheckUserPasswordById(999, "AnyPassword");
            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CheckUserPasswordById_ShouldReturnTrue_WhenPasswordIsCorrect()
        {
            // Arrange
            var testPassword = "Test@1234";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(testPassword);
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = "test@test.com",
                mdp_user = hashedPassword
            };
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act
            var result = await authService.CheckUserPasswordById(1, testPassword);
            // Assert
            Assert.True(result);
        }
        
        [Fact]
        public async Task ResetPassword_ShouldThrowInvalidOperationException_WhenSmtpIsDisabled()
        {
            // Arrange
            var testEmail = "test@test.com";
            var testToken = Guid.NewGuid().ToString();
            var testNewPassword = "NewPassword@123";
            var inMemorySettings = new Dictionary<string, string?> {
                {"SMTP:Enable", "false"}
            };
            _configuration.Setup(c => c["SMTP:Enable"]).Returns("false");
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            var resetRequest = new ResetPasswordRequest
            {
                Email = testEmail,
                Token = testToken,
                Password = testNewPassword
            };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await authService.ResetPassword(resetRequest)
            );
        }

        [Fact]
        public async Task ResetPassword_ShouldThrowInvalidOperationException_WhenTokenIsInvalid()
        {
            // Arrange
            var testEmail = "test@test.com";
            var testToken = Guid.NewGuid().ToString();
            var testNewPassword = "NewPassword@123";
            _configuration.Setup(c => c["SMTP:Enable"]).Returns("true");
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            var resetRequest = new ResetPasswordRequest
            {
                Email = testEmail,
                Token = testToken,
                Password = testNewPassword
            };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await authService.ResetPassword(resetRequest)
            );
        }

        [Fact]
        public async Task ResetPassword_ShouldResetPassword_WhenTokenIsValid()
        {
            // Arrange
            var testEmail = "test@test.com";
            var testToken = Guid.NewGuid();
            var testNewPassword = "NewPassword@123";
            _configuration.Setup(c => c["SMTP:Enable"]).Returns("true");
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = testEmail,
                mdp_user = BCrypt.Net.BCrypt.HashPassword("OldPassword@123"),
                reset_token = testToken,
                reset_token_expiration = DateTime.Now.AddHours(1)
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            var resetRequest = new ResetPasswordRequest
            {
                Email = testEmail,
                Token = testToken.ToString(),
                Password = testNewPassword
            };
            // Act
            await authService.ResetPassword(resetRequest);
            var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.email_user == testEmail);
            // Assert
            Assert.NotNull(updatedUser);
            Assert.True(BCrypt.Net.BCrypt.Verify(testNewPassword, updatedUser!.mdp_user));
            Assert.Null(updatedUser.reset_token);
            Assert.Null(updatedUser.reset_token_expiration);
            _smtpService.Verify(s => s.SendEmailAsync(
                testEmail,
                "Password changed",
                "Your password has been changed"
            ), Times.Once);
        }
        
        [Fact]
        public async Task ForgotPassword_ShouldThrowInvalidOperationException_WhenSmtpIsDisabled()
        {
            // Arrange
            var testEmail = "test@test.com";
            _configuration.Setup(c => c["SMTP:Enable"]).Returns("false");
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            var forgotRequest = new ForgotPasswordRequest
            {
                Email = testEmail
            };
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await authService.ForgotPassword(forgotRequest)
            );
        }

        [Fact]
        public async Task ForgotPassword_ShouldSendEmail_WhenUserExists()
        {
            // Arrange
            var testEmail = "test@test.com";
            _configuration.Setup(c => c["SMTP:Enable"]).Returns("true");
            using var context = new ApplicationDbContext(_dbContextOptions);
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = testEmail,
                mdp_user = BCrypt.Net.BCrypt.HashPassword("OldPassword@123")
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            var forgotRequest = new ForgotPasswordRequest
            {
                Email = testEmail
            };
            // Act
            await authService.ForgotPassword(forgotRequest);
            var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.email_user == testEmail);
            // Assert
            Assert.NotNull(updatedUser);
            Assert.NotNull(updatedUser!.reset_token);
            Assert.NotNull(updatedUser.reset_token_expiration);
            _smtpService.Verify(s => s.SendEmailAsync(
                testEmail,
                "Reset password",
                It.Is<string>(body => body.Contains(updatedUser.reset_token.ToString()!))
            ), Times.Once);
        }

        [Fact]
        public async Task ForgotPassword_ShouldNotThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var testEmail = "test@test.com";
            _configuration.Setup(c => c["SMTP:Enable"]).Returns("true");
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            var forgotRequest = new ForgotPasswordRequest
            {
                Email = testEmail
            };
            // Act & Assert
            var exception = await Record.ExceptionAsync(async () => 
                await authService.ForgotPassword(forgotRequest)
            );
            Assert.Null(exception);
        }

        /* [Fact]
        public async Task Logout_ShouldRevokeTokens()
        {
            // Arrange
            var testUserId = 1;
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act
            await authService.Logout(testUserId, "Test logout");
            // Assert
            _jwiService.Verify(j => j.RevokeAllAccessTokenByUser(testUserId, "Test logout"), Times.Once);
            _jwiService.Verify(j => j.RevokeAllRefreshTokenByUser(testUserId, "Test logout"), Times.Once);
        } */

        [Fact]
        public async Task LoginWithPassword_ShouldThrowUnauthorizedAccessException_WhenUserDoesNotExist()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Email = "test@test.com",
                Password = "Test@1234"
            };
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => 
                await authService.LoginWithPassword(loginRequest)
            );
        }

        [Fact]
        public async Task LoginWithPassword_ShouldThrowUnauthorizedAccessException_WhenPasswordIsIncorrect()
        {
            // Arrange
            var testEmail = "test@test.com";
            var testPassword = "Test@1234";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(testPassword);
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = testEmail,
                mdp_user = hashedPassword
            };
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var loginRequest = new LoginRequest
            {
                Email = testEmail,
                Password = "WrongPassword"
            };
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => 
                await authService.LoginWithPassword(loginRequest)
            );
        }

        [Fact]
        public async Task LoginWithPassword_ShouldReturnLoginResponse_WhenCredentialsAreCorrect()
        {
            // Arrange
            var testEmail = "test@test.com";
            var testPassword = "Test@1234";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(testPassword);
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = testEmail,
                mdp_user = hashedPassword
            };
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var loginRequest = new LoginRequest
            {
                Email = testEmail,
                Password = testPassword
            };
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act
            var result = await authService.LoginWithPassword(loginRequest);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(testEmail, result.user.email_user);
        }

        [Fact]
        public async Task RefreshJwt_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            var testClientId = 1;
            var testTokenId = Guid.NewGuid().ToString();
            _sessionService.Setup(s => s.GetClientId()).Returns(testClientId);
            _sessionService.Setup(s => s.GetTokenId()).Returns(testTokenId);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("password");
            _jwiService.Setup(j => j.GetSessionIdByTokenId(testTokenId, testClientId))
                .ReturnsAsync(Guid.NewGuid());
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => 
                await authService.RefreshJwt()
            );
        }

        [Fact]
        public async Task RefreshJwt_ShouldReturnLoginResponse_WhenUserExists()
        {
            // Arrange
            var testClientId = 1;
            var testTokenId = Guid.NewGuid().ToString();
            _sessionService.Setup(s => s.GetClientId()).Returns(testClientId);
            _sessionService.Setup(s => s.GetTokenId()).Returns(testTokenId);
            _sessionService.Setup(s => s.GetTokenAuthMethod()).Returns("password");
            var sessionId = Guid.NewGuid();
            _jwiService.Setup(j => j.GetSessionIdByTokenId(testTokenId, testClientId))
                .ReturnsAsync(sessionId);
            var user = new Users
            {
                id_user = testClientId,
                nom_user = "Test",
                prenom_user = "User",
                email_user = "test@test.com",
                mdp_user = BCrypt.Net.BCrypt.HashPassword("Test@1234")
            };
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            var authService = new AuthService(
                _mapper,
                context,
                _configuration.Object,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act
            var result = await authService.RefreshJwt();
            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.email_user, result.user.email_user);
        }

        [Fact]
        public async Task GetSSOAuthUrl_ShouldThrowArgumentException_WhenConfigurationIsInvalid()
        {
            // Arrange
            var ssoMethod = "Google";
            var ssoConfig = new Dictionary<string, string?>
            {
                {"OAuth:Google:ClientId", null},
                {"OAuth:Google:Authority", null},
                {"OAuth:Google:RedirectUri", null},
                {"OAuth:Google:Scope", null}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(ssoConfig)
                .Build();
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                configuration,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => 
                await authService.GetSSOAuthUrl(ssoMethod)
            );
        }

        [Fact]
        public async Task GetSSOAuthUrl_ShouldReturnSsoUrlResponse_WhenConfigurationIsValid()
        {
            // Arrange
            var ssoMethod = "Google";
            var ssoConfig = new Dictionary<string, string?>
            {
                {"OAuth:Google:ClientId", "test-client-id"},
                {"OAuth:Google:Authority", "https://accounts.google.com/o/oauth2/v2/auth"},
                {"OAuth:Google:RedirectUri", "https://example.com/oauth2/callback"},
                {"OAuth:Google:Scope", "openid email profile"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(ssoConfig)
                .Build();
            using var context = new ApplicationDbContext(_dbContextOptions);
            var authService = new AuthService(
                _mapper,
                context,
                configuration,
                _smtpService.Object,
                _sessionService.Object,
                _userService.Object,
                _jwtService.Object,
                _jwiService.Object
            );
            // Act
            var result = await authService.GetSSOAuthUrl(ssoMethod);
            // Assert
            Assert.NotNull(result);
            Assert.Contains("accounts.google.com", result.AuthUrl);
            Assert.False(string.IsNullOrEmpty(result.State));
        }

        //[Fact]
        //LoginWithSSO
    }
}