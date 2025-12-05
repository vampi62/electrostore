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
using electrostore.Services.JwtService;

namespace electrostore.Tests.Services
{
    public class JwtServiceTests
    {
        [Fact]
        public async Task GenerateToken_ShouldReturnValidJwt()
        {
            // Arrange
            var jwtSettings = new JwtSettings
            {
                Key = "ThisIsASecretKeyForJwtTokenGeneration12345",
                Issuer = "electrostore",
                Audience = "electrostore_users",
                ExpireDays = 7
            };
            var jwtService = new JwtService(Microsoft.Extensions.Options.Options.Create(jwtSettings));
            var user = new ReadUserDto
            {
                id_user = 1,
                prenom_user = "Test",
                nom_user = "User",
                email_user = "test@test.com",
                role_user = UserRole.User,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };
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