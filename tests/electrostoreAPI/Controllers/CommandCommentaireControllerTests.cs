using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.CommandCommentaireService;

namespace electrostore.Tests.Controllers
{
    public class CommandCommentaireControllerTests
    {
        private readonly Mock<ICommandCommentaireService> _service;
        private readonly CommandCommentaireController _controller;
        private readonly DefaultHttpContext _httpContext;

        public CommandCommentaireControllerTests()
        {
            _service = new Mock<ICommandCommentaireService>();

            _controller = new CommandCommentaireController(_service.Object);

            // Build HttpContext with headers and a user identity for Create
            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public async Task GetByCommand_ReturnsOk_WithHeader()
        {
            // Prepare response headers access
            var headersContext = new DefaultHttpContext();
            _controller.ControllerContext.HttpContext = headersContext;

            var list = new List<ReadExtendedCommandCommentaireDto>
            {
                new() { id_command_commentaire = 1, id_command = 1, id_user = 10, contenu_command_commentaire = "A", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_command_commentaire = 2, id_command = 1, id_user = 11, contenu_command_commentaire = "B", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetCommandsCommentairesByCommandId(1, 100, 0, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetCommandsCommentairesCountByCommandId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetCommandsCommentairesByCommandId(1);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedCommandCommentaireDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", headersContext.Response.Headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithComment()
        {
            var dto = new ReadExtendedCommandCommentaireDto { id_command_commentaire = 5, id_command = 2, id_user = 20, contenu_command_commentaire = "C", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetCommandsCommentaireById(5, null, 2, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetCommandsCommentaireById(2, 5);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedCommandCommentaireDto>(ok.Value);
            Assert.Equal(5, value.id_command_commentaire);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithComment()
        {
            // Add claims principal with nameidentifier
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "42") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            var created = new ReadCommandCommentaireDto { id_command_commentaire = 7, id_command = 3, id_user = 42, contenu_command_commentaire = "New", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateCommentaire(It.IsAny<CreateCommandCommentaireDto>())).ReturnsAsync(created);

            var res = await _controller.CreateCommentaire(3, new CreateCommandCommentaireByCommandDto { contenu_command_commentaire = "New" });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadCommandCommentaireDto>(createdRes.Value);
            Assert.Equal(7, value.id_command_commentaire);
            Assert.Equal(42, value.id_user);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdated()
        {
            var updated = new ReadCommandCommentaireDto { id_command_commentaire = 8, id_command = 4, id_user = 10, contenu_command_commentaire = "Upd", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateCommentaire(8, It.IsAny<UpdateCommandCommentaireDto>(), null, 4)).ReturnsAsync(updated);

            var res = await _controller.UpdateCommentaire(4, 8, new UpdateCommandCommentaireDto { contenu_command_commentaire = "Upd" });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadCommandCommentaireDto>(ok.Value);
            Assert.Equal(8, value.id_command_commentaire);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteCommentaire(9, null, 5)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteCommentaire(5, 9);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
