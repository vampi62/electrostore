using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using System.Threading.Tasks;
using electrostore.Controllers;
using electrostore.Dto;
using electrostore.Services.ConfigService;

namespace electrostore.Tests.Controllers
{
    public class ConfigControllerTests
    {
        [Fact]
        public async Task GetConfigs_ReturnsOk_WithConfig()
        {
            var service = new Mock<IConfigService>();
            var expected = new ReadConfig
            {
                smtp_enabled = true,
                mqtt_connected = true,
                ia_service_status = "online",
                demo_mode = false,
                max_length_url = 2048,
                max_length_commentaire = 1000,
                max_length_description = 2000,
                max_length_name = 255,
                max_length_type = 100,
                max_length_email = 320,
                max_length_ip = 45,
                max_length_reason = 500,
                max_length_status = 100,
                max_size_document_in_mb = 10,
                sso_available_providers = []
            };
            service.Setup(s => s.getAllConfig()).ReturnsAsync(expected);

            var controller = new ConfigController(service.Object);
            var res = await controller.GetConfigs();

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var value = Assert.IsType<ReadConfig>(ok.Value);
            Assert.Equal(expected, value);
        }
    }
}