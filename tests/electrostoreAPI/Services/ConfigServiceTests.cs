using MQTTnet;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Services.ConfigService;
using ElectrostoreAPI.Tests.Utils;
using System.Net;

namespace ElectrostoreAPI.Tests.Services
{
    public class ConfigServiceTests : TestBase
    {
        private readonly Mock<IMqttClient> _mqttClient;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;

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

            _configuration = BuildDefaultConfiguration();
        }

        private static IConfiguration BuildDefaultConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"SMTP:Enable", "true"},
                {"DemoMode", "true"},
                {"OAuth:Google:DisplayName", "Google"},
                {"OAuth:Google:IconUrl", "https://example.com/google-icon.png"},
                {"OAuth:Facebook:DisplayName", "Facebook"},
                {"OAuth:Facebook:IconUrl", "https://example.com/facebook-icon.png"}
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private ConfigService CreateService()
        {
            return new ConfigService(_configuration);
        }

        // --- getAllConfig ---

        [Fact]
        public async Task getAllConfig_shouldReturnAllConfigs()
        {
            // Arrange
            var configService = CreateService();

            // Act
            var result = await configService.getAllConfig();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ReadConfig>(result);
            Assert.Equal(_configuration.GetValue<bool>("DemoMode"), result.demo_mode);
            Assert.Equal(FieldLengths.MaxUrlLength, result.max_length_url);
            Assert.Equal(FieldLengths.MaxCommentLength, result.max_length_comment);
            Assert.Equal(FieldLengths.MaxDescriptionLength, result.max_length_description);
            Assert.Equal(FieldLengths.MaxNameLength,  result.max_length_name);
            Assert.Equal(FieldLengths.MaxTypeLength, result.max_length_type);
            Assert.Equal(FieldLengths.MaxEmailLength, result.max_length_email);
            Assert.Equal(FieldLengths.MaxIpLength, result.max_length_ip);
            Assert.Equal(FieldLengths.MaxReasonLength, result.max_length_reason);
            Assert.Equal(FieldLengths.MaxDocumentSizeMB, result.max_size_document_in_mb);
            var ssoProviders = _configuration.GetSection("OAuth").GetChildren().Select(provider => new SsoAvailableProvider
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
