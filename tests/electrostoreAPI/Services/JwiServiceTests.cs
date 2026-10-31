using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.JwiService;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.JwtService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class JwiServiceTests : TestBase
    {
        private readonly Mock<ISessionService> _sessionService;
        private readonly Mock<IJwtService> _jwtService;
        private readonly JwtSettings _jwtSettings;

        public JwiServiceTests()
        {
            _sessionService = new Mock<ISessionService>();
            _sessionService.Setup(s => s.GetClientIp()).Returns("192.168.1.1");
            _sessionService.Setup(s => s.GetClientId()).Returns(1);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            _jwtService = new Mock<IJwtService>();
            _jwtSettings = new JwtSettings
            {
                Key = "ThisIsASecretKeyForJwtTokenGeneration12345",
                Issuer = "ElectrostoreAPI",
                Audience = "electrostoreAPI_users",
                ExpireDays = 7
            };
        }

        private JwiService CreateService(ApplicationDbContext context)
        {
            return new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
        }

        // Helper method to generate a real JWT token
        private string GenerateJwtToken(Guid tokenId, string role, DateTime expiresAt)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString()),
                    new Claim("role", role)
                }),
                NotBefore = expiresAt.AddMinutes(-20), // NotBefore must be before Expires
                Expires = expiresAt,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static Models.UsersSessions BuildSession(int userId, Guid sessionId, DateTime? revokedAt = null)
        {
            return new Models.UsersSessions
            {
                id_user_session = sessionId,
                id_user = userId,
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                revoked_at = revokedAt
            };
        }

        // Adds a token to the context, in the given session (created if it does not exist yet) or in a new one
        private static Guid AddToken(ApplicationDbContext context, TokenType type, Guid tokenId, int userId, bool isRevoked, DateTime expiresAt, Guid? sessionId = null)
        {
            var id = sessionId ?? Guid.NewGuid();
            if (!context.UsersSessions.Local.Any(s => s.id_user_session == id) && !context.UsersSessions.Any(s => s.id_user_session == id))
            {
                context.UsersSessions.Add(BuildSession(userId, id));
            }
            context.JwtTokens.Add(new Models.JwtTokens
            {
                id_jwi = tokenId,
                id_user_session = id,
                token_type = type,
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                expires_at = expiresAt,
                revoked_at = isRevoked ? DateTime.UtcNow : null
            });
            return id;
        }

        private static Models.Users BuildUser(int id, UserRole role = UserRole.User)
        {
            return new Models.Users
            {
                id_user = id,
                firstname_user = "Test",
                name_user = "User",
                email_user = "test@test.com",
                role_user = role
            };
        }

        // --- SaveToken ---

        [Fact]
        public async Task SaveToken_ShouldSaveTokensToDatabase()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tokenId = Guid.NewGuid();
            var refreshTokenId = Guid.NewGuid();
            var expireAccessToken = DateTime.UtcNow.AddMinutes(15);
            var expireRefreshToken = DateTime.UtcNow.AddDays(7);

            var token = new Jwt
            {
                token = GenerateJwtToken(tokenId, "access", expireAccessToken),
                refresh_token = GenerateJwtToken(refreshTokenId, "refresh", expireRefreshToken),
                expire_date_token = expireAccessToken,
                expire_date_refresh_token = expireRefreshToken,
                token_id = tokenId,
                refresh_token_id = refreshTokenId,
                created_at = DateTime.UtcNow
            };
            var jwiService = CreateService(context);
            // Act
            await jwiService.SaveToken(token, userId: 1, reason: "login");
            // Assert
            var savedAccessToken = await context.JwtTokens.FirstOrDefaultAsync(jwt => jwt.id_jwi == token.token_id);
            var savedRefreshToken = await context.JwtTokens.FirstOrDefaultAsync(jwt => jwt.id_jwi == token.refresh_token_id);
            Assert.NotNull(savedAccessToken);
            Assert.NotNull(savedRefreshToken);
            Assert.Equal(TokenType.Access, savedAccessToken.token_type);
            Assert.Equal(TokenType.Refresh, savedRefreshToken.token_type);
            Assert.Equal("login", savedAccessToken.auth_method);
            Assert.Equal("login", savedRefreshToken.auth_method);
            Assert.Equal("192.168.1.1", savedAccessToken.created_by_ip);
            // both tokens belong to the one session created for the login
            var session = await context.UsersSessions.SingleAsync();
            Assert.Equal(session.id_user_session, savedAccessToken.id_user_session);
            Assert.Equal(session.id_user_session, savedRefreshToken.id_user_session);
            Assert.Equal(1, session.id_user);
            Assert.Equal("login", session.auth_method);
            Assert.Equal("192.168.1.1", session.created_by_ip);
            Assert.Null(session.id_user_external_login);
            Assert.Null(session.revoked_at);
        }

        // --- ValidateToken ---

        [Fact]
        public async Task ValidateToken_ShouldReturnFalse_ForRevokedToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: true, expiresAt: DateTime.UtcNow.AddMinutes(15));
            await context.SaveChangesAsync();
            var expireAt = DateTime.UtcNow.AddMinutes(15);
            var jwtToken = GenerateJwtToken(tokenId, "access", expireAt);

            // Act
            var isValid = jwiService.ValidateToken(jwtToken, "access");
            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task ValidateToken_ShouldReturnTrue_ForValidToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15));
            await context.SaveChangesAsync();
            var expireAt = DateTime.UtcNow.AddMinutes(15);
            var jwtToken = GenerateJwtToken(tokenId, "access", expireAt);

            // Act
            var isValid = jwiService.ValidateToken(jwtToken, "access");
            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateToken_ShouldReturnFalse_ForExpiredToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(-5)); // Expired 5 minutes ago
            await context.SaveChangesAsync();
            var expireAt = DateTime.UtcNow.AddMinutes(-5);
            var jwtToken = GenerateJwtToken(tokenId, "access", expireAt);

            // Act
            var isValid = jwiService.ValidateToken(jwtToken, "access");
            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task ValidateToken_ShouldReturnFalse_ForNonexistentToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var nonExistentTokenId = Guid.NewGuid();
            var expireAt = DateTime.UtcNow.AddMinutes(15);
            var jwtToken = GenerateJwtToken(nonExistentTokenId, "access", expireAt);
            await context.SaveChangesAsync();
            // Act
            var isValid = jwiService.ValidateToken(jwtToken, "access");
            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task ValidateToken_ShouldReturnFalse_ForInvalidSignature()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15));
            await context.SaveChangesAsync();
            var expireAt = DateTime.UtcNow.AddMinutes(15);
            // Generate token with different key to simulate invalid signature
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("DifferentSecretKeyForJwtTokenGeneration123");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString()),
                    new Claim("role", "access")
                }),
                Expires = expireAt,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);
            // Act
            var isValid = jwiService.ValidateToken(jwtToken, "access");
            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_ShouldReturnFalse_ForMalformedToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var malformedToken = "this.is.not.a.valid.token";
            // Act
            var isValid = jwiService.ValidateToken(malformedToken, "access");
            // Assert
            Assert.False(isValid);
        }

        // --- IsRevoked ---

        [Fact]
        public async Task IsRevoked_ShouldReturnTrue_ForRevokedToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: true, expiresAt: DateTime.UtcNow.AddMinutes(15));
            await context.SaveChangesAsync();
            // Act
            var isRevoked = jwiService.IsRevoked(tokenId.ToString(), "access");
            // Assert
            Assert.True(isRevoked);
        }

        [Fact]
        public async Task IsRevoked_ShouldReturnFalse_ForActiveToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15));
            await context.SaveChangesAsync();
            // Act
            var isRevoked = jwiService.IsRevoked(tokenId.ToString(), "access");
            // Assert
            Assert.False(isRevoked);
        }

        [Fact]
        public async Task IsRevoked_ShouldReturnTrue_WhenSessionIsRevoked()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            var sessionId = AddToken(context, TokenType.Access, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15));
            context.UsersSessions.Local.Single(s => s.id_user_session == sessionId).revoked_at = DateTime.UtcNow;
            await context.SaveChangesAsync();
            // Act
            var isRevoked = jwiService.IsRevoked(tokenId.ToString(), "access");
            // Assert
            Assert.True(isRevoked);
        }

        [Fact]
        public async Task IsRevoked_ShouldReturnTrue_WhenTokenTypeDoesNotMatchRole()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15));
            await context.SaveChangesAsync();
            // Act
            var isRevoked = jwiService.IsRevoked(tokenId.ToString(), "refresh");
            // Assert
            Assert.True(isRevoked);
        }

        [Fact]
        public void IsRevoked_ShouldReturnTrue_ForNonexistentToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var nonExistentTokenId = Guid.NewGuid();
            // Act
            var isRevoked = jwiService.IsRevoked(nonExistentTokenId.ToString(), "access");
            // Assert
            Assert.True(isRevoked);
        }

        [Fact]
        public void IsRevoked_ShouldReturnTrue_ForEmptyTokenId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            // Act
            var isRevoked = jwiService.IsRevoked(string.Empty, "access");
            // Assert
            Assert.True(isRevoked);
        }

        // --- RevokeAllAccessTokenByUser ---

        [Fact]
        public async Task RevokeAllAccessTokenByUser_ShouldRevokeAllTokens()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            AddToken(context, TokenType.Access, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15));
            AddToken(context, TokenType.Access, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(30));
            AddToken(context, TokenType.Access, Guid.NewGuid(), 2, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(30));
            await context.SaveChangesAsync();
            // Act
            await jwiService.RevokeAllAccessTokenByUser(userId, "test revoke");
            // Assert
            var revokedTokens = await context.JwtTokens
                .Where(jwt => jwt.Session!.id_user == userId && jwt.revoked_at != null)
                .ToListAsync();
            Assert.Equal(2, revokedTokens.Count);
            foreach (var token in revokedTokens)
            {
                Assert.Equal("test revoke", token.revoked_reason);
                Assert.Equal("192.168.1.1", token.revoked_by_ip);
            }
            // the other users' tokens and the sessions themselves are untouched
            Assert.Single(await context.JwtTokens.Where(jwt => jwt.revoked_at == null).ToListAsync());
            Assert.All(await context.UsersSessions.ToListAsync(), s => Assert.Null(s.revoked_at));
        }

        // --- RevokeAllRefreshTokenByUser ---

        [Fact]
        public async Task RevokeAllRefreshTokenByUser_ShouldRevokeAllTokens()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7));
            AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(14));
            AddToken(context, TokenType.Refresh, Guid.NewGuid(), 2, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(14));
            await context.SaveChangesAsync();
            // Act
            await jwiService.RevokeAllRefreshTokenByUser(userId, "test revoke");
            // Assert
            var revokedTokens = await context.JwtTokens
                .Where(jwt => jwt.Session!.id_user == userId && jwt.revoked_at != null)
                .ToListAsync();
            Assert.Equal(2, revokedTokens.Count);
            foreach (var token in revokedTokens)
            {
                Assert.Equal("test revoke", token.revoked_reason);
            }
            // the sessions can't be renewed anymore, the other users' ones are untouched
            var sessions = await context.UsersSessions.ToListAsync();
            Assert.Equal(2, sessions.Count(s => s.id_user == userId && s.revoked_at != null));
            Assert.Single(sessions, s => s.id_user == 2 && s.revoked_at == null);
        }

        // --- GetTokenSessionById ---

        [Fact]
        public async Task GetTokenSessionById_ShouldReturnSession_ForGivenSessionId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), sessionId: sessionId);
            AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(14), sessionId: sessionId);
            await context.SaveChangesAsync();
            // Act
            var session = await jwiService.GetTokenSessionById(sessionId.ToString(), userId);
            // Assert
            Assert.Equal(sessionId, session.session_id);
            Assert.Equal(DateTime.UtcNow.AddDays(14).Date, session.expires_at.Date);
            Assert.Equal("login", session.auth_method);
            Assert.Equal(userId, session.id_user);
            Assert.False(session.is_revoked);
        }

        [Fact]
        public async Task GetTokenSessionById_ShouldThrowKeyNotFoundException_ForInvalidSessionId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var invalidSessionId = Guid.NewGuid().ToString();
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await jwiService.GetTokenSessionById(invalidSessionId, userId);
            });
        }

        [Fact]
        public async Task GetTokenSessionById_ShouldThrowKeyNotFoundException_ForSessionOfAnotherUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var sessionId = Guid.NewGuid();
            AddToken(context, TokenType.Refresh, Guid.NewGuid(), 2, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), sessionId: sessionId);
            await context.SaveChangesAsync();
            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => jwiService.GetTokenSessionById(sessionId.ToString(), 1));
        }

        [Fact]
        public async Task GetTokenSessionById_ShouldThrowUnauthorizedAccessException_ForUnauthorizedUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var sessionId = Guid.NewGuid().ToString();
            _sessionService.Setup(s => s.GetClientId()).Returns(2); // Different user
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await jwiService.GetTokenSessionById(sessionId, userId);
            });
        }

        [Fact]
        public async Task GetTokenSessionById_ShouldAllowAdminUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), sessionId: sessionId);
            AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(14), sessionId: sessionId);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(2); // Different user
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            // Act
            var session = await jwiService.GetTokenSessionById(sessionId.ToString(), userId);
            // Assert
            Assert.Equal(sessionId, session.session_id);
            Assert.Equal(DateTime.UtcNow.AddDays(14).Date, session.expires_at.Date);
            Assert.False(session.is_revoked);
        }

        // --- RevokeSessionById ---

        [Fact]
        public async Task RevokeSessionById_ShouldRevokeSessionAndTokens_ForGivenSessionId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var sessionId = AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7));
            AddToken(context, TokenType.Access, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15), sessionId: sessionId);
            var otherSessionId = AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7));
            await context.SaveChangesAsync();
            // Act
            var revokedSession = await jwiService.RevokeSessionById(sessionId.ToString(), "user logout", userId);
            // Assert
            Assert.Equal(sessionId, revokedSession.session_id);
            Assert.True(revokedSession.is_revoked);
            Assert.Equal("user logout", revokedSession.revoked_reason);
            Assert.Equal("192.168.1.1", revokedSession.revoked_by_ip);
            var tokens = await context.JwtTokens.Where(jwt => jwt.id_user_session == sessionId).ToListAsync();
            Assert.Equal(2, tokens.Count);
            Assert.All(tokens, token =>
            {
                Assert.NotNull(token.revoked_at);
                Assert.Equal("user logout", token.revoked_reason);
            });
            var session = await context.UsersSessions.SingleAsync(s => s.id_user_session == sessionId);
            Assert.NotNull(session.revoked_at);
            Assert.Equal("192.168.1.1", session.revoked_by_ip);
            // the other session is untouched
            Assert.Null((await context.UsersSessions.SingleAsync(s => s.id_user_session == otherSessionId)).revoked_at);
            Assert.Null((await context.JwtTokens.SingleAsync(jwt => jwt.id_user_session == otherSessionId)).revoked_at);
        }

        [Fact]
        public async Task RevokeSessionById_ShouldThrowUnauthorizedAccessException_ForUnauthorizedUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var sessionId = Guid.NewGuid().ToString();
            _sessionService.Setup(s => s.GetClientId()).Returns(2); // Different user
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await jwiService.RevokeSessionById(sessionId, "user logout", userId);
            });
        }

        [Fact]
        public async Task RevokeSessionById_ShouldAllowAdminUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            context.Users.Add(BuildUser(userId));
            var sessionId = AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7));
            AddToken(context, TokenType.Access, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15), sessionId: sessionId);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(2); // Different user
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            // Act
            var revokedSession = await jwiService.RevokeSessionById(sessionId.ToString(), "admin revoke", userId);
            // Assert
            Assert.Equal(sessionId, revokedSession.session_id);
            Assert.True(revokedSession.is_revoked);
            var tokens = await context.JwtTokens.Where(jwt => jwt.id_user_session == sessionId).ToListAsync();
            Assert.Equal(2, tokens.Count);
            Assert.All(tokens, token =>
            {
                Assert.NotNull(token.revoked_at);
                Assert.Equal("admin revoke", token.revoked_reason);
            });
        }

        // --- RevokePairTokenByRefreshToken ---

        [Fact]
        public async Task RevokePairTokenByRefreshToken_ShouldRevokeTokens_ForGivenRefreshToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var refreshTokenId = Guid.NewGuid();
            var accessTokenId = Guid.NewGuid();
            context.Users.Add(BuildUser(userId));
            var sessionId = AddToken(context, TokenType.Refresh, refreshTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7));
            AddToken(context, TokenType.Access, accessTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15), sessionId: sessionId);
            await context.SaveChangesAsync();
            // Act
            await jwiService.RevokePairTokenByRefreshToken(refreshTokenId.ToString(), "token compromise", userId);
            // Assert
            var revokedRefreshToken = await context.JwtTokens.FirstOrDefaultAsync(jwt => jwt.id_jwi == refreshTokenId);
            var revokedAccessToken = await context.JwtTokens.FirstOrDefaultAsync(jwt => jwt.id_jwi == accessTokenId);
            Assert.NotNull(revokedRefreshToken?.revoked_at);
            Assert.Equal("token compromise", revokedRefreshToken?.revoked_reason);
            Assert.NotNull(revokedAccessToken?.revoked_at);
            Assert.Equal("token compromise", revokedAccessToken?.revoked_reason);
            // the session stays alive, a new pair is going to be saved in it
            Assert.Null((await context.UsersSessions.SingleAsync()).revoked_at);
        }

        [Fact]
        public async Task RevokePairTokenByRefreshToken_ShouldNotRevokeTokensOfOtherSessions()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var refreshTokenId = Guid.NewGuid();
            var otherTokenId = Guid.NewGuid();
            context.Users.Add(BuildUser(userId));
            AddToken(context, TokenType.Refresh, refreshTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7));
            AddToken(context, TokenType.Refresh, otherTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7));
            await context.SaveChangesAsync();
            // Act
            await jwiService.RevokePairTokenByRefreshToken(refreshTokenId.ToString(), "token compromise", userId);
            // Assert
            Assert.NotNull((await context.JwtTokens.SingleAsync(jwt => jwt.id_jwi == refreshTokenId)).revoked_at);
            Assert.Null((await context.JwtTokens.SingleAsync(jwt => jwt.id_jwi == otherTokenId)).revoked_at);
        }

        [Fact]
        public async Task RevokePairTokenByRefreshToken_ShouldThrowUnauthorizedAccessException_ForUnauthorizedUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var refreshTokenId = Guid.NewGuid();
            _sessionService.Setup(s => s.GetClientId()).Returns(2); // Different user
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.User);
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                await jwiService.RevokePairTokenByRefreshToken(refreshTokenId.ToString(), "token compromise", userId);
            });
        }

        [Fact]
        public async Task RevokePairTokenByRefreshToken_ShouldAllowAdminUser()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var refreshTokenId = Guid.NewGuid();
            var accessTokenId = Guid.NewGuid();
            context.Users.Add(BuildUser(userId, UserRole.Admin));
            var sessionId = AddToken(context, TokenType.Refresh, refreshTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7));
            AddToken(context, TokenType.Access, accessTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15), sessionId: sessionId);
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(2); // Different user
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            // Act
            await jwiService.RevokePairTokenByRefreshToken(refreshTokenId.ToString(), "admin revoke", userId);
            // Assert
            var revokedRefreshToken = await context.JwtTokens.FirstOrDefaultAsync(jwt => jwt.id_jwi == refreshTokenId);
            var revokedAccessToken = await context.JwtTokens.FirstOrDefaultAsync(jwt => jwt.id_jwi == accessTokenId);
            Assert.NotNull(revokedRefreshToken?.revoked_at);
            Assert.Equal("admin revoke", revokedRefreshToken?.revoked_reason);
            Assert.NotNull(revokedAccessToken?.revoked_at);
            Assert.Equal("admin revoke", revokedAccessToken?.revoked_reason);
        }

        // --- additional coverage ---

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenSettingsAreMissing()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var options = new Mock<IOptions<JwtSettings>>();
            options.Setup(o => o.Value).Returns((JwtSettings)null!);

            Assert.Throws<ArgumentNullException>(() => new JwiService(context, options.Object, _sessionService.Object));
        }

        [Fact]
        public async Task SaveToken_ShouldReuseProvidedSessionId()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var sessionId = Guid.NewGuid();
            context.UsersSessions.Add(BuildSession(1, sessionId));
            await context.SaveChangesAsync();
            var jwt = new Jwt
            {
                token = "a",
                refresh_token = "r",
                token_id = Guid.NewGuid(),
                refresh_token_id = Guid.NewGuid(),
                expire_date_token = DateTime.UtcNow.AddMinutes(15),
                expire_date_refresh_token = DateTime.UtcNow.AddDays(7)
            };

            await service.SaveToken(jwt, 1, "sso_oidc", sessionId);

            // no new session, the tokens are added to the existing one
            Assert.Single(await context.UsersSessions.ToListAsync());
            var tokens = await context.JwtTokens.ToListAsync();
            Assert.Equal(2, tokens.Count);
            Assert.All(tokens, token =>
            {
                Assert.Equal(sessionId, token.id_user_session);
                Assert.Equal("sso_oidc", token.auth_method);
            });
        }

        [Fact]
        public async Task SaveToken_ShouldThrowKeyNotFoundException_WhenProvidedSessionDoesNotExistForUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            var otherUserSessionId = Guid.NewGuid();
            context.UsersSessions.Add(BuildSession(2, otherUserSessionId));
            await context.SaveChangesAsync();
            var jwt = new Jwt
            {
                token = "a",
                refresh_token = "r",
                token_id = Guid.NewGuid(),
                refresh_token_id = Guid.NewGuid(),
                expire_date_token = DateTime.UtcNow.AddMinutes(15),
                expire_date_refresh_token = DateTime.UtcNow.AddDays(7)
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SaveToken(jwt, 1, "login", Guid.NewGuid()));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SaveToken(jwt, 1, "login", otherUserSessionId));
            Assert.Empty(await context.JwtTokens.ToListAsync());
        }

        [Fact]
        public async Task SaveToken_ShouldLinkSessionToExternalLogin()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);
            context.Users.Add(BuildUser(1));
            var externalLogin = new UsersExternalLogins { id_user = 1, provider = "oidc", provider_subject = "sub-1" };
            context.UsersExternalLogins.Add(externalLogin);
            await context.SaveChangesAsync();
            var jwt = new Jwt
            {
                token = "a",
                refresh_token = "r",
                token_id = Guid.NewGuid(),
                refresh_token_id = Guid.NewGuid(),
                expire_date_token = DateTime.UtcNow.AddMinutes(15),
                expire_date_refresh_token = DateTime.UtcNow.AddDays(7)
            };

            await service.SaveToken(jwt, 1, "sso_oidc", externalLoginId: externalLogin.id_user_external_login);

            var session = await context.UsersSessions.SingleAsync();
            Assert.Equal(externalLogin.id_user_external_login, session.id_user_external_login);
            Assert.Equal("sso_oidc", session.auth_method);
        }

        [Fact]
        public async Task ValidateToken_ShouldReturnFalse_WhenRoleClaimDoesNotMatch()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15));
            await context.SaveChangesAsync();
            var token = GenerateJwtToken(tokenId, "refresh", DateTime.UtcNow.AddMinutes(15));
            var service = CreateService(context);

            Assert.False(service.ValidateToken(token, "access"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ValidateToken_ShouldReturnFalse_WhenTokenIsEmpty(string? token)
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            Assert.False(service.ValidateToken(token!, "access"));
        }

        [Fact]
        public async Task IsRevoked_ShouldEvaluateRefreshTokens()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var active = Guid.NewGuid();
            var revoked = Guid.NewGuid();
            var expired = Guid.NewGuid();
            AddToken(context, TokenType.Refresh, active, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(1));
            AddToken(context, TokenType.Refresh, revoked, 1, isRevoked: true, expiresAt: DateTime.UtcNow.AddDays(1));
            AddToken(context, TokenType.Refresh, expired, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(-1));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            Assert.False(service.IsRevoked(active.ToString(), "refresh"));
            Assert.True(service.IsRevoked(revoked.ToString(), "refresh"));
            Assert.True(service.IsRevoked(expired.ToString(), "refresh"));
            Assert.True(service.IsRevoked(Guid.NewGuid().ToString(), "refresh"));
        }

        [Fact]
        public async Task IsRevoked_ShouldReturnTrue_ForExpiredAccessTokenAndUnknownRole()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var expired = Guid.NewGuid();
            AddToken(context, TokenType.Access, expired, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(-1));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            Assert.True(service.IsRevoked(expired.ToString(), "access"));
            Assert.True(service.IsRevoked(expired.ToString(), "something-else"));
        }

        // --- GetTokenSessionsByUserId ---

        private async Task SeedSessions(ApplicationDbContext context, int userId, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var sessionId = AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7 + i));
                AddToken(context, TokenType.Refresh, Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(14 + i), sessionId: sessionId);
            }
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetTokenSessionsByUserId_ShouldReturnOneEntryPerSession_AndOnlyReturnUserSessions()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            await SeedSessions(context, 1, 3);
            await SeedSessions(context, 2, 2);
            var service = CreateService(context);

            var result = await service.GetTokenSessionsByUserId(1);

            Assert.Equal(3, result.data.Count());
            Assert.Equal(3, result.pagination.total);
            Assert.False(result.pagination.has_more);
            Assert.All(result.data, s => Assert.Equal(1, s.id_user));
            // the session lifetime is the one of its latest refresh token
            Assert.All(result.data, s => Assert.True(s.expires_at >= DateTime.UtcNow.AddDays(14).AddMinutes(-1)));
        }

        [Fact]
        public async Task GetTokenSessionsByUserId_ShouldPaginate()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            await SeedSessions(context, 1, 3);
            var service = CreateService(context);

            var result = await service.GetTokenSessionsByUserId(1, limit: 2, offset: 0);

            Assert.Equal(2, result.data.Count());
            Assert.Equal(3, result.pagination.total);
            Assert.True(result.pagination.has_more);
            Assert.Equal(2, result.pagination.next_offset);
        }

        [Fact]
        public async Task GetTokenSessionsByUserId_ShouldApplyValidSortAndFallBackOnInvalidSort()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            await SeedSessions(context, 1, 2);
            var service = CreateService(context);

            var sorted = await service.GetTokenSessionsByUserId(1, sort: new SorterDto { field = "created_at", order = "asc" });
            var invalid = await service.GetTokenSessionsByUserId(1, sort: new SorterDto { field = "does_not_exist", order = "asc" });

            Assert.Equal(2, sorted.data.Count());
            Assert.Equal("created_at", sorted.sort!.Single().field);
            Assert.Equal(2, invalid.data.Count());
            Assert.Equal("created_at", invalid.sort!.Single().field);
            Assert.Equal("desc", invalid.sort!.Single().order);
        }

        [Fact]
        public async Task GetTokenSessionsByUserId_ShouldAllowAdminToViewOtherUsersSessions()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            await SeedSessions(context, 2, 1);
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            var service = CreateService(context);

            var result = await service.GetTokenSessionsByUserId(2);

            Assert.Single(result.data);
        }

        [Fact]
        public async Task GetTokenSessionsByUserId_ShouldThrowUnauthorizedAccessException_ForOtherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetTokenSessionsByUserId(2));
        }

        // --- GetSessionIdByTokenId ---

        [Fact]
        public async Task GetSessionIdByTokenId_ShouldReturnSessionId_WhenTokenExists()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var tokenId = Guid.NewGuid();
            var sessionId = AddToken(context, TokenType.Refresh, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            Assert.Equal(sessionId, await service.GetSessionIdByTokenId(tokenId.ToString(), 1));
        }

        [Fact]
        public async Task GetSessionIdByTokenId_ShouldReturnEmptyGuid_WhenTokenDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            Assert.Equal(Guid.Empty, await service.GetSessionIdByTokenId(Guid.NewGuid().ToString(), 1));
        }

        [Fact]
        public async Task GetSessionIdByTokenId_ShouldThrowUnauthorizedAccessException_ForOtherUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetSessionIdByTokenId(Guid.NewGuid().ToString(), 2));
        }

        // --- RevokeSessionById: missing session ---

        [Fact]
        public async Task RevokeSessionById_ShouldThrowKeyNotFoundException_WhenSessionDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RevokeSessionById(Guid.NewGuid().ToString(), "reason", 1));
        }

        // --- RevokePairTokenByRefreshToken: errors ---

        [Fact]
        public async Task RevokePairTokenByRefreshToken_ShouldThrowKeyNotFoundException_WhenUserDoesNotExist()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RevokePairTokenByRefreshToken(Guid.NewGuid().ToString(), "reason", 1));
        }

        [Fact]
        public async Task RevokePairTokenByRefreshToken_ShouldThrowKeyNotFoundException_WhenRefreshTokenDoesNotBelongToUser()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Refresh, tokenId, 2, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RevokePairTokenByRefreshToken(tokenId.ToString(), "reason", 1));
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RevokePairTokenByRefreshToken(Guid.NewGuid().ToString(), "reason", 1));
        }

        [Fact]
        public async Task RevokePairTokenByRefreshToken_ShouldThrowKeyNotFoundException_WhenTokenIsNotARefreshToken()
        {
            using var context = new ApplicationDbContext(_dbContextOptions);
            context.Users.Add(BuildUser(1));
            var tokenId = Guid.NewGuid();
            AddToken(context, TokenType.Access, tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(1));
            await context.SaveChangesAsync();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RevokePairTokenByRefreshToken(tokenId.ToString(), "reason", 1));
        }
    }
}
