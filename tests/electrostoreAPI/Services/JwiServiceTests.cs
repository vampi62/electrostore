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

        private static Models.JwiAccessTokens BuildAccessToken(Guid tokenId, int userId, bool isRevoked, DateTime expiresAt, Guid? sessionId = null)
        {
            return new Models.JwiAccessTokens
            {
                id_jwi_access = tokenId,
                session_id = sessionId ?? Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = isRevoked,
                expires_at = expiresAt
            };
        }

        private static Models.JwiRefreshTokens BuildRefreshToken(Guid tokenId, int userId, bool isRevoked, DateTime expiresAt, Guid? sessionId = null, Guid accessTokenId = default, DateTime? createdAt = null)
        {
            return new Models.JwiRefreshTokens
            {
                id_jwi_refresh = tokenId,
                id_jwi_access = accessTokenId,
                session_id = sessionId ?? Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = isRevoked,
                expires_at = expiresAt,
                created_at = createdAt ?? DateTime.UtcNow
            };
        }

        private static Models.Users BuildUser(int id, UserRole role = UserRole.User)
        {
            return new Models.Users
            {
                id_user = id,
                prenom_user = "Test",
                nom_user = "User",
                email_user = "test@test.com",
                mdp_user = "hashedpassword",
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
            var savedAccessToken = await context.JwiAccessTokens.FirstOrDefaultAsync(jwi => jwi.id_jwi_access == token.token_id);
            var savedRefreshToken = await context.JwiRefreshTokens.FirstOrDefaultAsync(jwi => jwi.id_jwi_refresh == token.refresh_token_id);
            Assert.NotNull(savedAccessToken);
            Assert.NotNull(savedRefreshToken);
            Assert.Equal("login", savedAccessToken.auth_method);
            Assert.Equal("login", savedRefreshToken.auth_method);
        }

        // --- ValidateToken ---

        [Fact]
        public async Task ValidateToken_ShouldReturnFalse_ForRevokedToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var tokenId = Guid.NewGuid();
            context.JwiAccessTokens.Add(BuildAccessToken(tokenId, 1, isRevoked: true, expiresAt: DateTime.UtcNow.AddMinutes(15)));
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
            context.JwiAccessTokens.Add(BuildAccessToken(tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15)));
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
            context.JwiAccessTokens.Add(BuildAccessToken(tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(-5))); // Expired 5 minutes ago
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
            context.JwiAccessTokens.Add(BuildAccessToken(tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15)));
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
            context.JwiAccessTokens.Add(BuildAccessToken(tokenId, 1, isRevoked: true, expiresAt: DateTime.UtcNow.AddMinutes(15)));
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
            context.JwiAccessTokens.Add(BuildAccessToken(tokenId, 1, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15)));
            await context.SaveChangesAsync();
            // Act
            var isRevoked = jwiService.IsRevoked(tokenId.ToString(), "access");
            // Assert
            Assert.False(isRevoked);
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
            context.JwiAccessTokens.AddRange(new List<Models.JwiAccessTokens>
            {
                BuildAccessToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15)),
                BuildAccessToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(30))
            });
            await context.SaveChangesAsync();
            // Act
            await jwiService.RevokeAllAccessTokenByUser(userId, "test revoke");
            // Assert
            var revokedTokens = await context.JwiAccessTokens
                .Where(jwi => jwi.id_user == userId && jwi.is_revoked)
                .ToListAsync();
            Assert.Equal(2, revokedTokens.Count);
            foreach (var token in revokedTokens)
            {
                Assert.Equal("test revoke", token.revoked_reason);
            }
        }

        // --- RevokeAllRefreshTokenByUser ---

        [Fact]
        public async Task RevokeAllRefreshTokenByUser_ShouldRevokeAllTokens()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            context.JwiRefreshTokens.AddRange(new List<Models.JwiRefreshTokens>
            {
                BuildRefreshToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7)),
                BuildRefreshToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(14))
            });
            await context.SaveChangesAsync();
            // Act
            await jwiService.RevokeAllRefreshTokenByUser(userId, "test revoke");
            // Assert
            var revokedTokens = await context.JwiRefreshTokens
                .Where(jwi => jwi.id_user == userId && jwi.is_revoked)
                .ToListAsync();
            Assert.Equal(2, revokedTokens.Count);
            foreach (var token in revokedTokens)
            {
                Assert.Equal("test revoke", token.revoked_reason);
            }
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
            context.JwiRefreshTokens.AddRange(new List<Models.JwiRefreshTokens>
            {
                BuildRefreshToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), sessionId: sessionId, createdAt: DateTime.UtcNow.AddDays(-1)),
                BuildRefreshToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(14), sessionId: sessionId, createdAt: DateTime.UtcNow)
            });
            await context.SaveChangesAsync();
            // Act
            var session = await jwiService.GetTokenSessionById(sessionId.ToString(), userId);
            // Assert
            Assert.Equal(sessionId, session.session_id);
            Assert.Equal(DateTime.UtcNow.AddDays(14).Date, session.expires_at.Date);
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
            context.JwiRefreshTokens.AddRange(new List<Models.JwiRefreshTokens>
            {
                BuildRefreshToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), sessionId: sessionId, createdAt: DateTime.UtcNow.AddDays(-1)),
                BuildRefreshToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(14), sessionId: sessionId, createdAt: DateTime.UtcNow)
            });
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
        public async Task RevokeSessionById_ShouldRevokeTokens_ForGivenSessionId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = CreateService(context);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            context.JwiRefreshTokens.Add(BuildRefreshToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), sessionId: sessionId));
            context.JwiAccessTokens.Add(BuildAccessToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15), sessionId: sessionId));
            await context.SaveChangesAsync();
            // Act
            var revokedSession = await jwiService.RevokeSessionById(sessionId.ToString(), "user logout", userId);
            // Assert
            Assert.Equal(sessionId, revokedSession.session_id);
            Assert.True(revokedSession.is_revoked);
            var revokedRefreshToken = await context.JwiRefreshTokens
                .FirstOrDefaultAsync(jwi => jwi.session_id == sessionId && jwi.id_user == userId);
            var revokedAccessToken = await context.JwiAccessTokens
                .FirstOrDefaultAsync(jwi => jwi.session_id == sessionId && jwi.id_user == userId);
            Assert.True(revokedRefreshToken?.is_revoked);
            Assert.Equal("user logout", revokedRefreshToken?.revoked_reason);
            Assert.True(revokedAccessToken?.is_revoked);
            Assert.Equal("user logout", revokedAccessToken?.revoked_reason);
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
            var sessionId = Guid.NewGuid();
            context.Users.Add(BuildUser(userId));
            context.JwiRefreshTokens.Add(BuildRefreshToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), sessionId: sessionId));
            context.JwiAccessTokens.Add(BuildAccessToken(Guid.NewGuid(), userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15), sessionId: sessionId));
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(2); // Different user
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            // Act
            var revokedSession = await jwiService.RevokeSessionById(sessionId.ToString(), "admin revoke", userId);
            // Assert
            Assert.Equal(sessionId, revokedSession.session_id);
            Assert.True(revokedSession.is_revoked);
            var revokedRefreshToken = await context.JwiRefreshTokens
                .FirstOrDefaultAsync(jwi => jwi.session_id == sessionId && jwi.id_user == userId);
            var revokedAccessToken = await context.JwiAccessTokens
                .FirstOrDefaultAsync(jwi => jwi.session_id == sessionId && jwi.id_user == userId);
            Assert.True(revokedRefreshToken?.is_revoked);
            Assert.Equal("admin revoke", revokedRefreshToken?.revoked_reason);
            Assert.True(revokedAccessToken?.is_revoked);
            Assert.Equal("admin revoke", revokedAccessToken?.revoked_reason);
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
            context.JwiRefreshTokens.Add(BuildRefreshToken(refreshTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), accessTokenId: accessTokenId));
            context.JwiAccessTokens.Add(BuildAccessToken(accessTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15)));
            await context.SaveChangesAsync();
            // Act
            await jwiService.RevokePairTokenByRefreshToken(refreshTokenId.ToString(), "token compromise", userId);
            // Assert
            var revokedRefreshToken = await context.JwiRefreshTokens
                .FirstOrDefaultAsync(jwi => jwi.id_jwi_refresh == refreshTokenId);
            var revokedAccessToken = await context.JwiAccessTokens
                .FirstOrDefaultAsync(jwi => jwi.id_jwi_access == accessTokenId);
            Assert.True(revokedRefreshToken?.is_revoked);
            Assert.Equal("token compromise", revokedRefreshToken?.revoked_reason);
            Assert.True(revokedAccessToken?.is_revoked);
            Assert.Equal("token compromise", revokedAccessToken?.revoked_reason);
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
            context.JwiRefreshTokens.Add(BuildRefreshToken(refreshTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddDays(7), accessTokenId: accessTokenId));
            context.JwiAccessTokens.Add(BuildAccessToken(accessTokenId, userId, isRevoked: false, expiresAt: DateTime.UtcNow.AddMinutes(15)));
            await context.SaveChangesAsync();
            _sessionService.Setup(s => s.GetClientId()).Returns(2); // Different user
            _sessionService.Setup(s => s.GetClientRole()).Returns(UserRole.Admin);
            // Act
            await jwiService.RevokePairTokenByRefreshToken(refreshTokenId.ToString(), "admin revoke", userId);
            // Assert
            var revokedRefreshToken = await context.JwiRefreshTokens
                .FirstOrDefaultAsync(jwi => jwi.id_jwi_refresh == refreshTokenId);
            var revokedAccessToken = await context.JwiAccessTokens
                .FirstOrDefaultAsync(jwi => jwi.id_jwi_access == accessTokenId);
            Assert.True(revokedRefreshToken?.is_revoked);
            Assert.Equal("admin revoke", revokedRefreshToken?.revoked_reason);
            Assert.True(revokedAccessToken?.is_revoked);
            Assert.Equal("admin revoke", revokedAccessToken?.revoked_reason);
        }
    }
}
