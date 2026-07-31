using ElectrostoreNOTIF.Services.WebPushService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreNOTIF.Tests.Services;

public class WebPushServiceTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static WebPushService CreateService(Dictionary<string, string?> values)
    {
        return new WebPushService(BuildConfiguration(values), new Mock<ILogger<WebPushService>>().Object);
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenPublicKeyIsMissing()
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["VAPID:PrivateKey"] = "private-key"
        };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => CreateService(values));

        // Assert
        Assert.Contains("VAPID:PublicKey", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenPrivateKeyIsMissing()
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["VAPID:PublicKey"] = "public-key"
        };

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => CreateService(values));

        // Assert
        Assert.Contains("VAPID:PrivateKey", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenKeysAreProvided()
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["VAPID:PublicKey"] = "public-key",
            ["VAPID:PrivateKey"] = "private-key"
        };

        // Act
        var exception = Record.Exception(() => CreateService(values));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_ShouldDoNothing_WhenVapidIsDisabled()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["VAPID:PublicKey"] = "public-key",
            ["VAPID:PrivateKey"] = "private-key",
            ["VAPID:Enable"] = "false"
        });

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.SendAsync("https://push.example.com/endpoint", "p256dh", "auth", "Title", "Body"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_ShouldDoNothing_WhenVapidEnableIsMissing()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["VAPID:PublicKey"] = "public-key",
            ["VAPID:PrivateKey"] = "private-key"
        });

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.SendAsync("https://push.example.com/endpoint", "p256dh", "auth", "Title", "Body"));

        // Assert
        Assert.Null(exception);
    }
}
