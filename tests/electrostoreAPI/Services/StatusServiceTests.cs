using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MQTTnet;
using Moq;
using Moq.Protected;
using Xunit;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ElectrostoreAPI;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Services.StatusService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class StatusServiceTests : TestBase
    {
        private const string IaUrl = "http://ia.test/health";
        private const string NotifUrl = "http://notif.test/health";
        private const string CronUrl = "http://cron.test/health";
        private const string WorkerUrl = "http://worker.test/health";

        private readonly Mock<IMqttClient> _mqttClient;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly Mock<IKafkaProducerService> _kafkaProducerService;

        public StatusServiceTests()
        {
            _mqttClient = new Mock<IMqttClient>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            // StatusService issues 4 concurrent health checks that each call CreateClient() and
            // mutate Timeout on the returned instance - a shared HttpClient would throw
            // "already started one or more requests" under concurrency, so hand out a fresh
            // instance (wrapping the same mocked handler) per call, like the real factory does.
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(_mockHttpMessageHandler.Object));
            _kafkaProducerService = new Mock<IKafkaProducerService>();
        }

        private StatusService CreateService(ApplicationDbContext context, IConfiguration configuration)
        {
            return new StatusService(_mqttClient.Object, configuration, _mockHttpClientFactory.Object, context, _kafkaProducerService.Object);
        }

        private static IConfiguration BuildConfiguration(bool demoMode = false, bool track17Enable = false)
        {
            var settings = new Dictionary<string, string?>
            {
                ["IAServiceHealthUrl"] = IaUrl,
                ["NotifServiceHealthUrl"] = NotifUrl,
                ["CRONServiceHealthUrl"] = CronUrl,
                ["WORKERServiceHealthUrl"] = WorkerUrl,
                ["DemoMode"] = demoMode.ToString(),
                ["Track17:Enable"] = track17Enable.ToString()
            };
            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        private void SetupHealthResponse(string url, string json)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString() == url),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(json) });
        }

        private void SetupHealthFailure(string url)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString() == url),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("connection refused"));
        }

        private void SetupAllHealthy()
        {
            SetupHealthResponse(IaUrl, "{\"status\":\"healthy\",\"training_in_progress\":2}");
            SetupHealthResponse(NotifUrl, "{\"status\":\"healthy\",\"smtp\":true,\"webPush\":false}");
            SetupHealthResponse(CronUrl, "{\"status\":\"healthy\"}");
            SetupHealthResponse(WorkerUrl, "{\"status\":\"healthy\"}");
        }

        // --- GetStatus ---

        [Fact]
        public async Task GetStatus_ShouldAggregateAllHealthyServices()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetupAllHealthy();
            _mqttClient.Setup(m => m.IsConnected).Returns(true);
            _kafkaProducerService.Setup(k => k.IsConnectedAsync()).ReturnsAsync(true);
            var service = CreateService(context, BuildConfiguration());
            // Act
            var result = await service.GetStatus();
            // Assert
            Assert.Equal("healthy", result.api_status);
            Assert.True(result.db_connected);
            Assert.True(result.mqtt_connected);
            Assert.True(result.kafka_connected);
            Assert.Equal("healthy", result.ia_status);
            Assert.Equal(2, result.ia_training_in_progress);
            Assert.Equal("healthy", result.notif_status);
            Assert.True(result.notif_smtp);
            Assert.False(result.notif_webPush);
            Assert.Equal("healthy", result.cron_status);
            Assert.Equal("healthy", result.worker_status);
        }

        [Fact]
        public async Task GetStatus_ShouldReturnDemoApiStatus_WhenDemoModeEnabled()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetupAllHealthy();
            var service = CreateService(context, BuildConfiguration(demoMode: true));
            // Act
            var result = await service.GetStatus();
            // Assert
            Assert.Equal("demo", result.api_status);
        }

        [Fact]
        public async Task GetStatus_ShouldReturnUnreachable_WhenServiceCallFails()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetupHealthFailure(IaUrl);
            SetupHealthResponse(NotifUrl, "{\"status\":\"healthy\"}");
            SetupHealthResponse(CronUrl, "{\"status\":\"healthy\"}");
            SetupHealthResponse(WorkerUrl, "{\"status\":\"healthy\"}");
            var service = CreateService(context, BuildConfiguration());
            // Act
            var result = await service.GetStatus();
            // Assert
            Assert.Equal("unreachable", result.ia_status);
        }

        [Fact]
        public async Task GetStatus_ShouldReflectMqttAndKafkaDisconnectedState()
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetupAllHealthy();
            _mqttClient.Setup(m => m.IsConnected).Returns(false);
            _kafkaProducerService.Setup(k => k.IsConnectedAsync()).ReturnsAsync(false);
            var service = CreateService(context, BuildConfiguration());
            // Act
            var result = await service.GetStatus();
            // Assert
            Assert.False(result.mqtt_connected);
            Assert.False(result.kafka_connected);
        }

        [Theory]
        [InlineData(true, "enabled")]
        [InlineData(false, "disabled")]
        public async Task GetStatus_ShouldReflectTrack17ConfigState(bool track17Enable, string expected)
        {
            // Arrange
            using var context = new ApplicationDbContext(_dbContextOptions);
            SetupAllHealthy();
            var service = CreateService(context, BuildConfiguration(track17Enable: track17Enable));
            // Act
            var result = await service.GetStatus();
            // Assert
            Assert.Equal(expected, result.external_services["17Track"]);
        }
    }
}
