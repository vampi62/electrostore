using Microsoft.EntityFrameworkCore;
using MQTTnet;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using electrostore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.ConfigService;
using electrostore.Tests.Utils;
using System.Net;
using System.Net.Http;

namespace electrostore.Tests.Services
{
    public class ConfigServiceTests : TestBase
    {
        private Mock<IMqttClient> _mqttClient;
        private readonly IConfiguration _configuration;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;

        public ConfigServiceTests()
        {
            _mqttClient = new Mock<IMqttClient>();
            _mqttClient.Setup(m => m.IsConnected).Returns(true);
            
            // Mock HttpMessageHandler pour simuler les réponses HTTP
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"status\":\"healthy\"}")
                });
            
            // Mock IHttpClientFactory
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
            
            var inMemorySettings = new Dictionary<string, string?> {
                {"SMTP:Enable", "true"},
                {"DemoMode", "true"},
                {"OAuth:Google:DisplayName", "Google"},
                {"OAuth:Google:IconUrl", "https://example.com/google-icon.png"},
                {"OAuth:Facebook:DisplayName", "Facebook"},
                {"OAuth:Facebook:IconUrl", "https://example.com/facebook-icon.png"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }


        [Fact]
        public async Task getAllConfig_shouldReturnAllConfigs()
        {
            // Arrange
            var configService = new ConfigService(_mqttClient.Object, _configuration, _mockHttpClientFactory.Object);

            // Act
            var result = await configService.getAllConfig();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ReadConfig>(result);
            Assert.Equal(_configuration["SMTP:Enable"] == "true", result.smtp_enabled);
            Assert.Equal(_mqttClient.Object.IsConnected, result.mqtt_connected);
            Assert.Equal(_configuration.GetValue<bool>("DemoMode"), result.demo_mode);
            Assert.Equal(Constants.MaxUrlLength, result.max_length_url);
            Assert.Equal(Constants.MaxCommentaireLength, result.max_length_commentaire);
            Assert.Equal(Constants.MaxDescriptionLength, result.max_length_description);
            Assert.Equal(Constants.MaxNameLength,  result.max_length_name);
            Assert.Equal(Constants.MaxTypeLength, result.max_length_type);
            Assert.Equal(Constants.MaxEmailLength, result.max_length_email);
            Assert.Equal(Constants.MaxIpLength, result.max_length_ip);
            Assert.Equal(Constants.MaxReasonLength, result.max_length_reason);
            Assert.Equal(Constants.MaxStatusLength, result.max_length_status);
            Assert.Equal(Constants.MaxDocumentSizeMB, result.max_size_document_in_mb);
            var ssoProviders = _configuration.GetSection("OAuth").GetChildren().Select(provider => new SSOAvailableProvider
            {
                provider = provider.Key,
                display_name = provider.GetValue<string>("DisplayName") ?? string.Empty,
                icon_url = provider.GetValue<string>("IconUrl") ?? string.Empty
            }).ToList();
            Assert.NotNull(result.sso_available_providers);
            Assert.Equal(ssoProviders.Count, result.sso_available_providers.Count);
            for (int i = 0; i < ssoProviders.Count; i++)
            {
                Assert.NotNull(result.sso_available_providers[i]);
                Assert.Equal(ssoProviders[i].provider, result.sso_available_providers[i].provider);
                Assert.Equal(ssoProviders[i].display_name, result.sso_available_providers[i].display_name);
                Assert.Equal(ssoProviders[i].icon_url, result.sso_available_providers[i].icon_url);
            }
        }
    }
}