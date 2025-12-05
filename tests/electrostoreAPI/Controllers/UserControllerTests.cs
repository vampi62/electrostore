using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Services.UserService;

namespace electrostore.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly UserController _controller;
        private readonly Mock<HttpContext> _mockHttpContext;
        private readonly Mock<HttpResponse> _mockHttpResponse;
        private readonly HeaderDictionary _headerDictionary;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            
            // Set up mock HttpContext and Response
            _mockHttpContext = new Mock<HttpContext>();
            _mockHttpResponse = new Mock<HttpResponse>();
            _headerDictionary = new HeaderDictionary();
            
            _mockHttpResponse.Setup(r => r.Headers).Returns(_headerDictionary);
            _mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);
            
            _controller = new UserController(_mockUserService.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _mockHttpContext.Object
            };
        }

        [Fact]
        public async Task GetUsers_ReturnsOkResult_WithListOfUsers()
        {
            // Arrange
            var users = new List<ReadExtendedUserDto>
            {
                new ReadExtendedUserDto { id_user = 1, nom_user = "Test User 1", prenom_user = "User1", email_user = "test1@example.com", role_user = UserRole.Admin, created_at = DateTime.Now, updated_at = DateTime.Now,
                    projets_commentaires_count = 0, commands_commentaires_count = 0 },
                new ReadExtendedUserDto { id_user = 2, nom_user = "Test User 2", prenom_user = "User2", email_user = "test2@example.com", role_user = UserRole.User, created_at = DateTime.Now, updated_at = DateTime.Now,
                    projets_commentaires_count = 0, commands_commentaires_count = 0 }
            };
            _mockUserService.Setup(service => service.GetUsers(100, 0, null, null))
                .ReturnsAsync(users);
            _mockUserService.Setup(service => service.GetUsersCount())
                .ReturnsAsync(users.Count);

            // Act
            var result = await _controller.GetUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<ReadExtendedUserDto>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count());
        }

        [Fact]
        public async Task GetUserById_ReturnsOkResult_WithUser()
        {
            // Arrange
            var userId = 1;
            var user = new ReadExtendedUserDto { id_user = userId, nom_user = "Test User 1", prenom_user = "User1", email_user = "test1@example.com", role_user = UserRole.Admin, created_at = DateTime.Now, updated_at = DateTime.Now,
                    projets_commentaires_count = 0, commands_commentaires_count = 0 };
            _mockUserService.Setup(service => service.GetUserById(userId, It.IsAny<List<string>>()))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetUserById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<ReadExtendedUserDto>(okResult.Value);
            Assert.Equal(userId, returnedUser.id_user);
        }

        [Fact]
        public async Task CreateUser_ReturnsCreatedAtActionResult_WithCreatedUser()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                nom_user = "New User",
                prenom_user = "User",
                email_user = "test@test.com",
                mdp_user = "Password123",
                role_user = UserRole.User
            };
            var createdUser = new ReadUserDto
            {
                id_user = 3,
                nom_user = "New User",
                prenom_user = "User",
                email_user = "test@test.com",
                role_user = UserRole.User,
                created_at = DateTime.Now,
                updated_at = DateTime.Now
            };
            _mockUserService.Setup(service => service.CreateUser(createUserDto))
                .ReturnsAsync(createdUser);
            // Act
            var result = await _controller.CreateUser(createUserDto);
            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedUser = Assert.IsType<ReadUserDto>(createdAtActionResult.Value);
            Assert.Equal(createdUser.id_user, returnedUser.id_user);
        }

        [Fact]
        public async Task UpdateUser_ReturnsOkResult_WithUpdatedUser()
        {
            // Arrange
            var userId = 1;
            var updateUserDto = new UpdateUserDto
            {
                nom_user = "Updated User",
                prenom_user = "User",
                email_user = "test@test.com",
                role_user = UserRole.Admin
            };
            var updatedUser = new ReadUserDto
            {
                id_user = userId,
                nom_user = "Updated User",
                prenom_user = "User",
                email_user = "test@test.com",
                role_user = UserRole.Admin,
                created_at = DateTime.Now.AddDays(-1),
                updated_at = DateTime.Now
            };
            _mockUserService.Setup(service => service.UpdateUser(userId, updateUserDto))
                .ReturnsAsync(updatedUser);
            // Act
            var result = await _controller.UpdateUser(userId, updateUserDto);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<ReadUserDto>(okResult.Value);
            Assert.Equal(updatedUser.id_user, returnedUser.id_user);
        }

        [Fact]
        public async Task DeleteUser_ReturnsNoContentResult()
        {
            // Arrange
            var userId = 1;
            _mockUserService.Setup(service => service.DeleteUser(userId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
    }
}