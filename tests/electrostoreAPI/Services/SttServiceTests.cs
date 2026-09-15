using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElectrostoreAPI.Services.SttService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class SttServiceTests : TestBase
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler = new();
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();

        public SttServiceTests()
        {
            _mockHttpClientFactory.Setup(f => f.CreateClient(SttService.HttpClientName))
                .Returns(() => new HttpClient(_mockHttpMessageHandler.Object) { BaseAddress = new Uri("http://stt.test/") });
        }

        private SttService CreateService(IConfiguration configuration) =>
            new(_mockHttpClientFactory.Object, configuration, CreateLogger<SttService>());

        private static IConfiguration BuildConfiguration(bool enabled = true, string? endpoint = null)
        {
            var values = new Dictionary<string, string?> { ["Stt:Enable"] = enabled.ToString() };
            if (endpoint is not null)
            {
                values["Stt:Endpoint"] = endpoint;
            }
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static Mock<IFormFile> BuildAudioFile(string fileName = "audio.wav", string contentType = "audio/wav")
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("fake-audio-bytes")));
            return file;
        }

        private void SetupResponse(HttpStatusCode statusCode, string body)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode, Content = new StringContent(body) });
        }

        // --- IsEnabled ---

        [Fact]
        public void IsEnabled_ShouldReflectConfiguration()
        {
            var enabledService = CreateService(BuildConfiguration(enabled: true));
            var disabledService = CreateService(BuildConfiguration(enabled: false));

            Assert.True(enabledService.IsEnabled);
            Assert.False(disabledService.IsEnabled);
        }

        // --- TranscribeAsync ---

        [Fact]
        public async Task TranscribeAsync_ShouldThrowInvalidOperationException_WhenDisabled()
        {
            var service = CreateService(BuildConfiguration(enabled: false));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.TranscribeAsync(BuildAudioFile().Object));
        }

        [Fact]
        public async Task TranscribeAsync_ShouldReturnTranscribedText_OnSuccess()
        {
            SetupResponse(HttpStatusCode.OK, "{\"text\":\"hello world\"}");
            var service = CreateService(BuildConfiguration());

            var result = await service.TranscribeAsync(BuildAudioFile().Object);

            Assert.Equal("hello world", result);
        }

        [Fact]
        public async Task TranscribeAsync_ShouldReturnEmptyString_WhenResponseHasNoText()
        {
            SetupResponse(HttpStatusCode.OK, "{}");
            var service = CreateService(BuildConfiguration());

            var result = await service.TranscribeAsync(BuildAudioFile().Object);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task TranscribeAsync_ShouldThrowHttpRequestException_WhenResponseIsNotSuccessful()
        {
            SetupResponse(HttpStatusCode.InternalServerError, "boom");
            var service = CreateService(BuildConfiguration());

            await Assert.ThrowsAsync<HttpRequestException>(() => service.TranscribeAsync(BuildAudioFile().Object));
        }

        [Fact]
        public async Task TranscribeAsync_ShouldPropagateHttpRequestException_WhenSendFails()
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("connection refused"));
            var service = CreateService(BuildConfiguration());

            await Assert.ThrowsAsync<HttpRequestException>(() => service.TranscribeAsync(BuildAudioFile().Object));
        }
    }
}
