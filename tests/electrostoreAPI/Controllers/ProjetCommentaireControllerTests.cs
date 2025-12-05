using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ProjetCommentaireService;

namespace electrostore.Tests.Controllers
{
    public class ProjetCommentaireControllerTests
    {
        private readonly Mock<IProjetCommentaireService> _service;
        private readonly ProjetCommentaireController _controller;
        private readonly DefaultHttpContext _httpContext;

        public ProjetCommentaireControllerTests()
        {
            _service = new Mock<IProjetCommentaireService>();

            _controller = new ProjetCommentaireController(_service.Object);

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public async Task GetByProjet_ReturnsOk_WithHeader()
        {
            var ctx = new DefaultHttpContext();
            _controller.ControllerContext.HttpContext = ctx;

            var list = new List<ReadExtendedProjetCommentaireDto>
            {
                new() { id_projet_commentaire = 1, id_projet = 1, id_user = 10, contenu_projet_commentaire = "A", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow },
                new() { id_projet_commentaire = 2, id_projet = 1, id_user = 11, contenu_projet_commentaire = "B", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            };
            _service.Setup(s => s.GetProjetCommentairesByProjetId(1, 100, 0, null)).ReturnsAsync(list);
            _service.Setup(s => s.GetProjetCommentairesCountByProjetId(1)).ReturnsAsync(list.Count);

            var res = await _controller.GetProjetCommentairesByProjetId(1);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<ReadExtendedProjetCommentaireDto>>(ok.Value);
            Assert.Equal(2, value.Count());
            Assert.Equal("2", ctx.Response.Headers["X-Total-Count"].ToString());
        }

        [Fact]
        public async Task GetById_ReturnsOk_WithComment()
        {
            var dto = new ReadExtendedProjetCommentaireDto { id_projet_commentaire = 5, id_projet = 2, id_user = 20, contenu_projet_commentaire = "C", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.GetProjetCommentairesById(5, null, 2, It.IsAny<List<string>>())).ReturnsAsync(dto);

            var res = await _controller.GetProjetCommentairesById(2, 5);

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadExtendedProjetCommentaireDto>(ok.Value);
            Assert.Equal(5, value.id_projet_commentaire);
        }

        [Fact]
        public async Task Create_ReturnsCreated_WithComment()
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "77") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _httpContext.User = new ClaimsPrincipal(identity);

            var created = new ReadProjetCommentaireDto { id_projet_commentaire = 7, id_projet = 3, id_user = 77, contenu_projet_commentaire = "New", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.CreateProjetCommentaire(It.IsAny<CreateProjetCommentaireDto>())).ReturnsAsync(created);

            var res = await _controller.AddProjetCommentaire(3, new CreateProjetCommentaireByProjetDto { contenu_projet_commentaire = "New" });

            var createdRes = Assert.IsType<CreatedAtActionResult>(res.Result);
            var value = Assert.IsType<ReadProjetCommentaireDto>(createdRes.Value);
            Assert.Equal(77, value.id_user);
            Assert.Equal(7, value.id_projet_commentaire);
        }

        [Fact]
        public async Task Update_ReturnsOk_WithUpdated()
        {
            var updated = new ReadProjetCommentaireDto { id_projet_commentaire = 8, id_projet = 4, id_user = 10, contenu_projet_commentaire = "Upd", created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow };
            _service.Setup(s => s.UpdateProjetCommentaire(8, It.IsAny<UpdateProjetCommentaireDto>(), null, 4)).ReturnsAsync(updated);

            var res = await _controller.UpdateProjetCommentaire(4, 8, new UpdateProjetCommentaireDto { contenu_projet_commentaire = "Upd" });

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadProjetCommentaireDto>(ok.Value);
            Assert.Equal(8, value.id_projet_commentaire);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            _service.Setup(s => s.DeleteProjetCommentaire(9, null, 5)).Returns(Task.CompletedTask);
            var res = await _controller.DeleteProjetCommentaire(5, 9);
            Assert.IsType<NoContentResult>(res);
        }
    }
}
