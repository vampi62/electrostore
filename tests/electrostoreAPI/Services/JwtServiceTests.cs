using Microsoft.Extensions.Options;
using Xunit;
using System;
using System.Threading.Tasks;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Services.JwtService;

namespace ElectrostoreAPI.Tests.Services
{
    public class JwtServiceTests
    {
        private readonly JwtSettings _jwtSettings;

        public JwtServiceTests()
        {
            _jwtSettings = new JwtSettings
            {
                Key = "ThisIsASecretKeyForJwtTokenGeneration12345",
                Issuer = "ElectrostoreAPI",
                Audience = "electrostoreAPI_users",
                ExpireDays = 7
            };
        }

        private JwtService CreateService()
        {
            return new JwtService(Options.Create(_jwtSettings));
        }

        private static ReadUserDto BuildUser(int id = 1, UserRole role = UserRole.User)
        {
            return new ReadUserDto
            {
                id_user = id,
                prenom_user = "Test",
                nom_user = "User",
                email_user = "test@test.com",
                role_user = role,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };
        }

        // --- GenerateToken ---

        [Fact]
        public async Task GenerateToken_ShouldReturnValidJwt()
        {
            // Arrange
            var jwtService = CreateService();
            var user = BuildUser();
            var reason = "login";
            // Act
            var jwt = await jwtService.GenerateToken(user, reason);
            // Assert
            Assert.NotNull(jwt);
            Assert.False(string.IsNullOrEmpty(jwt.token));
            Assert.False(string.IsNullOrEmpty(jwt.refresh_token));
            Assert.True(jwt.expire_date_token > DateTime.UtcNow);
            Assert.True(jwt.expire_date_refresh_token > DateTime.UtcNow);
        }
    }
}
