using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class SessionServiceTests : TestBase
    {
        private static SessionService CreateService(Mock<IHttpContextAccessor> httpContextAccessor)
        {
            return new SessionService(httpContextAccessor.Object);
        }

        // --- GetClientIp ---

        [Fact]
        public void GetClientIp_ShouldReturnCorrectIp_FromXForwardedForHeader()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                headers: new Dictionary<string, string>
                {
                    { "X-Forwarded-For", "192.168.1.1,192.168.2.1" }
                },
                remoteIp: "192.168.10.1"
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var clientIp = sessionService.GetClientIp();
            // Assert
            Assert.Equal("192.168.1.1", clientIp);
        }

        [Fact]
        public void GetClientIp_ShouldReturnCorrectIp_FromXRealIpHeader()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                headers: new Dictionary<string, string>
                {
                    { "X-Real-IP", "192.168.1.1" }
                },
                remoteIp: "192.168.10.1"
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var clientIp = sessionService.GetClientIp();
            // Assert
            Assert.Equal("192.168.1.1", clientIp);
        }

        [Fact]
        public void GetClientIp_ShouldReturnCorrectIp_FromRemoteIpAddress()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                headers: new Dictionary<string, string>(),
                remoteIp: "192.168.10.1"
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var clientIp = sessionService.GetClientIp();
            // Assert
            Assert.Equal("192.168.10.1", clientIp);
        }

        [Fact]
        public void GetClientIp_ShouldReturnEmptyString_WhenNoIpAvailable()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                headers: new Dictionary<string, string>(),
                remoteIp: null
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var clientIp = sessionService.GetClientIp();
            // Assert
            Assert.Equal(string.Empty, clientIp);
        }

        // --- GetClientRole ---

        [Fact]
        public void GetClientRole_ShouldReturnUserRole_WhenNoClaimsPresent()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                claims: new Dictionary<string, string>()
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var userRole = sessionService.GetClientRole();
            // Assert
            Assert.Equal(UserRole.User, userRole);
        }

        [Fact]
        public void GetClientRole_ShouldReturnCorrectUserRole_FromClaims()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                claims: new Dictionary<string, string>
                {
                    { "user_role", "admin" }
                }
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var userRole = sessionService.GetClientRole();
            // Assert
            Assert.Equal(UserRole.Admin, userRole);
        }

        // --- GetClientId ---

        [Fact]
        public void GetClientId_ShouldReturnZero_WhenNoClaimsPresent()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                claims: new Dictionary<string, string>()
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var clientId = sessionService.GetClientId();
            // Assert
            Assert.Equal(0, clientId);
        }

        [Fact]
        public void GetClientId_ShouldReturnCorrectId_FromClaims()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                claims: new Dictionary<string, string>
                {
                    { ClaimTypes.NameIdentifier, "42" }
                }
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var clientId = sessionService.GetClientId();
            // Assert
            Assert.Equal(42, clientId);
        }

        // --- GetTokenId ---

        [Fact]
        public void GetTokenId_ShouldReturnEmptyString_WhenNoClaimsPresent()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                claims: new Dictionary<string, string>()
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var tokenId = sessionService.GetTokenId();
            // Assert
            Assert.Equal(string.Empty, tokenId);
        }

        [Fact]
        public void GetTokenId_ShouldReturnCorrectId_FromClaims()
        {
            // Arrange
            var expectedGuid = Guid.NewGuid().ToString();
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                claims: new Dictionary<string, string>
                {
                    { JwtRegisteredClaimNames.Jti, expectedGuid }
                }
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var tokenId = sessionService.GetTokenId();
            // Assert
            Assert.Equal(expectedGuid, tokenId);
        }

        // --- GetTokenAuthMethod ---

        [Fact]
        public void GetTokenAuthMethod_ShouldReturnEmptyString_WhenNoClaimsPresent()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                claims: new Dictionary<string, string>()
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var authMethod = sessionService.GetTokenAuthMethod();
            // Assert
            Assert.Equal(string.Empty, authMethod);
        }

        [Fact]
        public void GetTokenAuthMethod_ShouldReturnCorrectMethod_FromClaims()
        {
            // Arrange
            var httpContextAccessor = HttpContextAccessorMockFactory.Create(
                claims: new Dictionary<string, string>
                {
                    { ClaimTypes.AuthenticationMethod, "password" }
                }
            );
            var sessionService = CreateService(httpContextAccessor);
            // Act
            var authMethod = sessionService.GetTokenAuthMethod();
            // Assert
            Assert.Equal("password", authMethod);
        }
    }
}
