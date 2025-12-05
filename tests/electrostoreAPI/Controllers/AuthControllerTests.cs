using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.AuthService;

namespace electrostore.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _service;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _service = new Mock<IAuthService>();
            _controller = new AuthController(_service.Object);
        }

        [Fact]
        public async Task GetSSOAuthUrl_ReturnsOk()
        {
            var resp = new SsoUrlResponse { AuthUrl = "http://sso/authorize", State = "abc" };
            _service.Setup(s => s.GetSSOAuthUrl("google")).ReturnsAsync(resp);

            var result = await _controller.GetSSOAuthUrl("google");
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<SsoUrlResponse>(ok.Value);
            Assert.Equal("abc", value.State);
        }

        [Fact]
        public async Task SSOCallback_ReturnsOk_WithLoginResponse()
        {
            var login = new LoginResponse
            {
                token = "t",
                expire_date_token = DateTime.UtcNow.AddHours(1),
                refresh_token = "r",
                expire_date_refresh_token = DateTime.UtcNow.AddDays(7),
                user = new ReadUserDto { id_user = 1, nom_user = "N", prenom_user = "P", email_user = "e@m.com", role_user = electrostore.Enums.UserRole.User, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.LoginWithSSO("google", It.IsAny<SsoLoginRequest>())).ReturnsAsync(login);

            var result = await _controller.SSOCallback("google", new SsoLoginRequest { Code = "c", State = "s" });
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<LoginResponse>(ok.Value);
            Assert.Equal("t", value.token);
        }

        [Fact]
        public async Task Login_ReturnsOk_WithLoginResponse()
        {
            var login = new LoginResponse
            {
                token = "t",
                expire_date_token = DateTime.UtcNow.AddHours(1),
                refresh_token = "r",
                expire_date_refresh_token = DateTime.UtcNow.AddDays(7),
                user = new ReadUserDto { id_user = 2, nom_user = "N2", prenom_user = "P2", email_user = "u2@m.com", role_user = electrostore.Enums.UserRole.User, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.LoginWithPassword(It.IsAny<LoginRequest>())).ReturnsAsync(login);

            var result = await _controller.Login(new LoginRequest { Email = "a@b.com", Password = "Passw0rd!" });
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<LoginResponse>(ok.Value);
            Assert.Equal("r", value.refresh_token);
        }

        [Fact]
        public async Task RefreshToken_ReturnsOk()
        {
            var login = new LoginResponse
            {
                token = "t2",
                expire_date_token = DateTime.UtcNow.AddHours(1),
                refresh_token = "r2",
                expire_date_refresh_token = DateTime.UtcNow.AddDays(7),
                user = new ReadUserDto { id_user = 3, nom_user = "N3", prenom_user = "P3", email_user = "u3@m.com", role_user = electrostore.Enums.UserRole.User, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.RefreshJwt()).ReturnsAsync(login);

            var result = await _controller.RefreshToken();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<LoginResponse>(ok.Value);
            Assert.Equal("t2", value.token);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsOk()
        {
            _service.Setup(s => s.ForgotPassword(It.IsAny<ForgotPasswordRequest>())).Returns(Task.CompletedTask);
            var result = await _controller.ForgotPassword(new ForgotPasswordRequest { Email = "a@b.com" });
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ReturnsOk()
        {
            _service.Setup(s => s.ResetPassword(It.IsAny<ResetPasswordRequest>())).Returns(Task.CompletedTask);
            var result = await _controller.ResetPassword(new ResetPasswordRequest { Email = "a@b.com", Token = "t", Password = "Passw0rd!" });
            Assert.IsType<OkResult>(result);
        }
    }
}
