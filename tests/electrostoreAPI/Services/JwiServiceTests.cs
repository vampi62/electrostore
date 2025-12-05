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
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.JwiService;
using electrostore.Services.SessionService;
using electrostore.Services.JwtService;
using electrostore.Tests.Utils;

namespace electrostore.Tests.Services
{
    public class JwiServiceTests : TestBase
    {
        private Mock<ISessionService> _sessionService;
        private Mock<IJwtService> _jwtService;
        private JwtSettings _jwtSettings;
    
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
                Issuer = "electrostore",
                Audience = "electrostore_users",
                ExpireDays = 7
            };
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
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

        [Fact]
        public async Task ValidateToken_ShouldReturnFalse_ForRevokedToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var tokenId = Guid.NewGuid();
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = tokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = 1,
                is_revoked = true,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var tokenId = Guid.NewGuid();
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = tokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = 1,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var tokenId = Guid.NewGuid();
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = tokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = 1,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddMinutes(-5) // Expired 5 minutes ago
            });
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var tokenId = Guid.NewGuid();
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = tokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = 1,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
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
        public async Task ValidateToken_ShouldReturnFalse_ForMalformedToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var malformedToken = "this.is.not.a.valid.token";
            // Act
            var isValid = jwiService.ValidateToken(malformedToken, "access");
            // Assert
            Assert.False(isValid);
        }
        
        [Fact]
        public async Task IsRevoked_ShouldReturnTrue_ForRevokedToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var tokenId = Guid.NewGuid();
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = tokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = 1,
                is_revoked = true,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var tokenId = Guid.NewGuid();
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = tokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = 1,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
            await context.SaveChangesAsync();
            // Act
            var isRevoked = jwiService.IsRevoked(tokenId.ToString(), "access");
            // Assert
            Assert.False(isRevoked);
        }

        [Fact]
        public async Task IsRevoked_ShouldReturnTrue_ForNonexistentToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var nonExistentTokenId = Guid.NewGuid();
            // Act
            var isRevoked = jwiService.IsRevoked(nonExistentTokenId.ToString(), "access");
            // Assert
            Assert.True(isRevoked);
        }

        [Fact]
        public async Task IsRevoked_ShouldReturnTrue_ForEmptyTokenId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            // Act
            var isRevoked = jwiService.IsRevoked(string.Empty, "access");
            // Assert
            Assert.True(isRevoked);
        }

        [Fact]
        public async Task RevokeAllAccessTokenByUser_ShouldRevokeAllTokens()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            context.JwiAccessTokens.AddRange(new List<Models.JwiAccessTokens>
            {
                new Models.JwiAccessTokens
                {
                    id_jwi_access = Guid.NewGuid(),
                    session_id = Guid.NewGuid(),
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddMinutes(15)
                },
                new Models.JwiAccessTokens
                {
                    id_jwi_access = Guid.NewGuid(),
                    session_id = Guid.NewGuid(),
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddMinutes(30)
                }
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

        [Fact]
        public async Task RevokeAllRefreshTokenByUser_ShouldRevokeAllTokens()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            context.JwiRefreshTokens.AddRange(new List<Models.JwiRefreshTokens>
            {
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = Guid.NewGuid(),
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(7)
                },
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = Guid.NewGuid(),
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(14)
                }
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
        
        [Fact]
        public async Task GetTokenSessionsByUserId_ShouldReturnSessions_ForGivenUserId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            context.JwiRefreshTokens.AddRange(new List<Models.JwiRefreshTokens>
            {
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = sessionId,
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(7),
                    created_at = DateTime.UtcNow.AddDays(-1)
                },
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = sessionId,
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(14),
                    created_at = DateTime.UtcNow
                }
            });
            await context.SaveChangesAsync();
            // Act
            var sessions = await jwiService.GetTokenSessionsByUserId(userId, limit: 10, offset: 0);
            // Assert
            Assert.Single(sessions);
            var session = sessions.First();
            Assert.Equal(sessionId, session.session_id);
            Assert.Equal(DateTime.UtcNow.AddDays(14).Date, session.expires_at.Date);
            Assert.False(session.is_revoked);
        }

        [Fact]
        public async Task GetTokenSessionsCountByUserId_ShouldReturnCorrectCount_ForGivenUserId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            context.JwiRefreshTokens.AddRange(new List<Models.JwiRefreshTokens>
            {
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = sessionId,
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(7),
                    created_at = DateTime.UtcNow.AddDays(-1)
                },
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = sessionId,
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(14),
                    created_at = DateTime.UtcNow
                }
            });
            await context.SaveChangesAsync();
            // Act
            var count = await jwiService.GetTokenSessionsCountByUserId(userId);
            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetTokenSessionById_ShouldReturnSession_ForGivenSessionId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            context.JwiRefreshTokens.AddRange(new List<Models.JwiRefreshTokens>
            {
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = sessionId,
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(7),
                    created_at = DateTime.UtcNow.AddDays(-1)
                },
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = sessionId,
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(14),
                    created_at = DateTime.UtcNow
                }
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            context.JwiRefreshTokens.AddRange(new List<Models.JwiRefreshTokens>
            {
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = sessionId,
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(7),
                    created_at = DateTime.UtcNow.AddDays(-1)
                },
                new Models.JwiRefreshTokens
                {
                    id_jwi_refresh = Guid.NewGuid(),
                    session_id = sessionId,
                    auth_method = "login",
                    created_by_ip = "192.168.1.1",
                    id_user = userId,
                    is_revoked = false,
                    expires_at = DateTime.UtcNow.AddDays(14),
                    created_at = DateTime.UtcNow
                }
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

        [Fact]
        public async Task RevokeSessionById_ShouldRevokeTokens_ForGivenSessionId()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            context.JwiRefreshTokens.Add(new Models.JwiRefreshTokens
            {
                id_jwi_refresh = Guid.NewGuid(),
                session_id = sessionId,
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddDays(7)
            });
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = Guid.NewGuid(),
                session_id = sessionId,
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            var sessionId = Guid.NewGuid();
            context.Users.Add(new Models.Users
            {
                id_user = userId,
                prenom_user = "Test",
                nom_user = "User",
                email_user = "test@test.com",
                mdp_user = "hashedpassword",
                role_user = UserRole.User
            });
            context.JwiRefreshTokens.Add(new Models.JwiRefreshTokens
            {
                id_jwi_refresh = Guid.NewGuid(),
                session_id = sessionId,
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddDays(7)
            });
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = Guid.NewGuid(),
                session_id = sessionId,
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
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

        [Fact]
        public async Task RevokePairTokenByRefreshToken_ShouldRevokeTokens_ForGivenRefreshToken()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            var refreshTokenId = Guid.NewGuid();
            var accessTokenId = Guid.NewGuid();
            context.Users.Add(new Models.Users
            {
                id_user = userId,
                prenom_user = "Test",
                nom_user = "User",
                email_user = "test@test.com",
                mdp_user = "hashedpassword",
                role_user = UserRole.User
            });
            context.JwiRefreshTokens.Add(new Models.JwiRefreshTokens
            {
                id_jwi_refresh = refreshTokenId,
                id_jwi_access = accessTokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddDays(7)
            });
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = accessTokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
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
            var jwiService = new JwiService(context, Options.Create(_jwtSettings), _sessionService.Object);
            var userId = 1;
            var refreshTokenId = Guid.NewGuid();
            var accessTokenId = Guid.NewGuid();
            context.Users.Add(new Models.Users
            {
                id_user = userId,
                prenom_user = "Test",
                nom_user = "User",
                email_user = "test@test.com",
                mdp_user = "hashedpassword",
                role_user = UserRole.Admin
            });
            context.JwiRefreshTokens.Add(new Models.JwiRefreshTokens
            {
                id_jwi_refresh = refreshTokenId,
                id_jwi_access = accessTokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddDays(7)
            });
            context.JwiAccessTokens.Add(new Models.JwiAccessTokens
            {
                id_jwi_access = accessTokenId,
                session_id = Guid.NewGuid(),
                auth_method = "login",
                created_by_ip = "192.168.1.1",
                id_user = userId,
                is_revoked = false,
                expires_at = DateTime.UtcNow.AddMinutes(15)
            });
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