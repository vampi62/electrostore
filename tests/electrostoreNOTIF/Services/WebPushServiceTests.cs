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

    // Freshly generated (via WebPush's own VapidHelper.GenerateVapidKeys()) throwaway test keypair -
    // not tied to any real deployment - used only so VapidDetails construction succeeds.
    private static Dictionary<string, string?> EnabledVapidValues() => new()
    {
        ["VAPID:PublicKey"] = "BLlwbWnHyXySQ6_MXzqmfIB8Qs72kdgI3j0a2L0MAMz65sIa-xnTyNj-UDax1b-d-heJOep4vwo1OttXqVS02Yk",
        ["VAPID:PrivateKey"] = "Ch-bCoWZqAOnD6UM4D-Xl1PZhlW8FMSzl1wRON5u29c",
        ["VAPID:Enable"] = "true"
    };

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenPushServiceIsUnreachable()
    {
        // VAPID enabled drives execution into the real send attempt (WebPushClient isn't
        // injected/mockable). Port 1 has nobody listening, so the connection is refused
        // immediately, exercising SendAsync's generic catch-and-wrap branch deterministically.
        // Arrange
        var service = CreateService(EnabledVapidValues());

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.SendAsync("http://127.0.0.1:1/push-endpoint", "p256dh-key", "auth-key", "Title", "Body"));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("Failed to send push notification", exception!.Message);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenPushServiceIsUnreachable_WithData()
    {
        // Same as above but exercising the optional "data" payload branch.
        // Arrange
        var service = CreateService(EnabledVapidValues());
        var data = new Dictionary<string, string> { ["orderId"] = "123" };

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.SendAsync("http://127.0.0.1:1/push-endpoint", "p256dh-key", "auth-key", "Title", "Body", data));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }
}
