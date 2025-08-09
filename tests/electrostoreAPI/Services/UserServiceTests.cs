using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using electrostore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.UserService;
using electrostore.Services.SMTPService;
using electrostore.Services.SessionService;
using electrostore.Services.JwiService;
using electrostore.Services.JwtService;

namespace electrostore.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ISMTPService> _mockSmtpService;
        private readonly Mock<ISessionService> _mockSessionService;
        private readonly Mock<IJwiService> _mockJwiService;
        private readonly JwtService _jwtService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;

        public UserServiceTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockSmtpService = new Mock<ISMTPService>();
            _mockSessionService = new Mock<ISessionService>();
            _mockJwiService = new Mock<IJwiService>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Create a real JwtService with test settings
            var jwtSettings = Options.Create(new JwtSettings
            {
                Key = "test_key_for_jwt_authentication_that_is_long_enough",
                Issuer = "test",
                Audience = "test",
                ExpireDays = 1
            });
            _jwtService = new JwtService(jwtSettings);

            // Set up in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task LoginUserPassword_ShouldReturnTokens_WhenCredentialsAreValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add a test user to the database
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = "test@example.com",
                mdp_user = BCrypt.Net.BCrypt.HashPassword("Password123"),
                role_user = UserRole.User
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userService = new UserService(
                _mockMapper.Object,
                context,
                _mockConfiguration.Object,
                _mockSmtpService.Object,
                _mockSessionService.Object,
                _mockJwiService.Object,
                _jwtService
            );

            var loginRequest = new LoginRequest
            {
                Email = "test@example.com",
                Password = "Password123"
            };

            var userDto = new ReadUserDto
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = "test@example.com",
                role_user = UserRole.User,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            var tokenResponse = new JWT
            {
                token = "access_token",
                refresh_token = "refresh_token",
                expire_date_token = DateTime.Now.AddMinutes(15),
                expire_date_refresh_token = DateTime.Now.AddDays(7),
                token_id = Guid.NewGuid(),
                refresh_token_id = Guid.NewGuid(),
                created_at = DateTime.Now
            };

            _mockMapper.Setup(m => m.Map<ReadUserDto>(It.IsAny<Users>()))
                .Returns(userDto);

            // Using real JwtService instead of mock
            // Act
            var result = await userService.LoginUserPassword(loginRequest);

            // Assert
            Assert.NotNull(result);
            // Since we're using a real JwtService, we can't predict the exact token values
            Assert.NotNull(result.token);
            Assert.NotEmpty(result.token);
            Assert.NotNull(result.refresh_token);
            Assert.NotEmpty(result.refresh_token);
            Assert.True(result.expire_date_token > DateTime.Now);
            Assert.True(result.expire_date_refresh_token > DateTime.Now);
            Assert.Equal(userDto, result.user);

            _mockJwiService.Verify(j => j.SaveToken(It.IsAny<JWT>(), user.id_user, It.IsAny<Guid?>()), Times.Once);
            _mockSmtpService.Verify(s => s.SendEmailAsync(
                user.email_user,
                "Login",
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task RefreshJwt_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add a test user to the database
            var user = new Users
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = "test@example.com",
                mdp_user = BCrypt.Net.BCrypt.HashPassword("Password123"),
                role_user = UserRole.User
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userService = new UserService(
                _mockMapper.Object,
                context,
                _mockConfiguration.Object,
                _mockSmtpService.Object,
                _mockSessionService.Object,
                _mockJwiService.Object,
                _jwtService
            );

            var userDto = new ReadUserDto
            {
                id_user = 1,
                nom_user = "Test",
                prenom_user = "User",
                email_user = "test@example.com",
                role_user = UserRole.User,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };

            var tokenResponse = new JWT
            {
                token = "access_token",
                refresh_token = "refresh_token",
                expire_date_token = DateTime.Now.AddMinutes(15),
                expire_date_refresh_token = DateTime.Now.AddDays(7),
                token_id = Guid.NewGuid(),
                refresh_token_id = Guid.NewGuid(),
                created_at = DateTime.Now
            };

            _mockSessionService.Setup(s => s.GetClientId())
                .Returns(1);
            _mockSessionService.Setup(s => s.GetTokenId())
                .Returns("old_token_id");
            _mockJwiService.Setup(j => j.GetSessionIdByTokenId("old_token_id", 1))
                .ReturnsAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            _mockMapper.Setup(m => m.Map<ReadUserDto>(It.IsAny<Users>()))
                .Returns(userDto);
            
            // Using real JwtService instead of mock
            // Act
            var result = await userService.RefreshJwt();

            // Assert
            Assert.NotNull(result);
            // Since we're using a real JwtService, we can't predict the exact token values
            Assert.NotNull(result.token);
            Assert.NotEmpty(result.token);
            Assert.NotNull(result.refresh_token);
            Assert.NotEmpty(result.refresh_token);
            Assert.True(result.expire_date_token > DateTime.Now);
            Assert.True(result.expire_date_refresh_token > DateTime.Now);
            Assert.Equal(userDto, result.user);

            _mockJwiService.Verify(j => j.RevokePairTokenByRefreshToken("old_token_id", "User refresh token", 1), Times.Once);
            _mockJwiService.Verify(j => j.SaveToken(It.IsAny<JWT>(), user.id_user, It.IsAny<Guid?>()), Times.Once);
        }

        // Performance benchmark test
        [Fact]
        public async Task GetUsers_Performance_Benchmark()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            
            // Add test users to the database
            for (int i = 1; i <= 100; i++)
            {
                context.Users.Add(new Users
                {
                    id_user = i,
                    nom_user = $"User{i}",
                    prenom_user = $"Test{i}",
                    email_user = $"user{i}@example.com",
                    mdp_user = BCrypt.Net.BCrypt.HashPassword("Password123"),
                    role_user = UserRole.User
                });
            }
            await context.SaveChangesAsync();

            var userService = new UserService(
                _mockMapper.Object,
                context,
                _mockConfiguration.Object,
                _mockSmtpService.Object,
                _mockSessionService.Object,
                _mockJwiService.Object,
                _jwtService
            );

            _mockMapper.Setup(m => m.Map<ReadExtendedUserDto>(It.IsAny<Users>()))
                .Returns((Users u) => new ReadExtendedUserDto
                {
                    id_user = u.id_user,
                    nom_user = u.nom_user,
                    prenom_user = u.prenom_user,
                    email_user = u.email_user,
                    role_user = u.role_user
                });

            // Act
            var startTime = DateTime.Now;
            var result = await userService.GetUsers(100, 0);
            var endTime = DateTime.Now;
            var executionTime = (endTime - startTime).TotalMilliseconds;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Count());
            
            // Log performance metrics
            Console.WriteLine($"[BENCHMARK] GetUsers execution time: {executionTime}ms for 100 users");
            
            // Performance assertion - should be reasonably fast
            Assert.True(executionTime < 1000, $"GetUsers took too long: {executionTime}ms");
        }
    }
}