using ElectrostoreNOTIF.Services.EmailSenderService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreNOTIF.Tests.Services;

public class EmailSenderServiceTests
{
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static EmailSenderService CreateService(Dictionary<string, string?> values)
    {
        return new EmailSenderService(BuildConfiguration(values), new Mock<ILogger<EmailSenderService>>().Object);
    }

    [Fact]
    public async Task SendAsync_ShouldDoNothing_WhenSmtpIsDisabled()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "false"
        });

        // Act
        var exception = await Record.ExceptionAsync(() => service.SendAsync("user@example.com", "Subject", "Body"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowArgumentException_WhenRecipientIsEmpty()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true"
        });

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SendAsync("", "Subject", "Body"));
    }

    [Theory]
    [InlineData("user@localhost")]
    [InlineData("user@local")]
    [InlineData("user@localhost.local")]
    public async Task SendAsync_ShouldDoNothing_WhenRecipientIsLocalAddress(string to)
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true"
        });

        // Act
        var exception = await Record.ExceptionAsync(() => service.SendAsync(to, "Subject", "Body"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenFromAndUsernameAreMissing()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true"
        });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync("user@example.com", "Subject", "Body"));

        // Assert
        Assert.Contains("SMTP:From", exception.Message);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenHostIsMissing()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true",
            ["SMTP:From"] = "noreply@example.com"
        });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync("user@example.com", "Subject", "Body"));

        // Assert
        Assert.Contains("SMTP:Host", exception.Message);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenUsernameIsMissing()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true",
            ["SMTP:From"] = "noreply@example.com",
            ["SMTP:Host"] = "smtp.example.com"
        });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync("user@example.com", "Subject", "Body"));

        // Assert
        Assert.Contains("SMTP:Username", exception.Message);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenPasswordIsMissing()
    {
        // Arrange
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true",
            ["SMTP:From"] = "noreply@example.com",
            ["SMTP:Host"] = "smtp.example.com",
            ["SMTP:Username"] = "smtp-user"
        });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync("user@example.com", "Subject", "Body"));

        // Assert
        Assert.Contains("SMTP:Password", exception.Message);
    }

    // Once every required setting is present, SendAsync attempts a real SMTP connection
    // (SmtpClient isn't injected/mockable). Port 1 has nobody listening, so the connection is
    // refused immediately, exercising the generic catch-and-wrap branch deterministically and fast.

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenSmtpServerIsUnreachable_OnStartTlsPort()
    {
        // Arrange - any port other than 465 selects SecureSocketOptions.StartTls
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true",
            ["SMTP:From"] = "noreply@example.com",
            ["SMTP:Host"] = "127.0.0.1",
            ["SMTP:Port"] = "1",
            ["SMTP:Username"] = "smtp-user",
            ["SMTP:Password"] = "smtp-password"
        });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync("user@example.com", "Subject", "Body"));

        // Assert
        Assert.Contains("Failed to send e-mail to user@example.com", exception.Message);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenSmtpServerIsUnreachable_OnSslOnConnectPort()
    {
        // Arrange - port 465 selects SecureSocketOptions.SslOnConnect
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true",
            ["SMTP:From"] = "noreply@example.com",
            ["SMTP:Host"] = "127.0.0.1",
            ["SMTP:Port"] = "465",
            ["SMTP:Username"] = "smtp-user",
            ["SMTP:Password"] = "smtp-password"
        });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync("user@example.com", "Subject", "Body"));

        // Assert
        Assert.Contains("Failed to send e-mail to user@example.com", exception.Message);
    }

    [Fact]
    public async Task SendAsync_ShouldSkipAuthentication_WhenUsernameIsWhitespace()
    {
        // Arrange - a whitespace-only username passes the "?? throw" null-check but should skip
        // the AuthenticateAsync call; the connection still fails since nothing listens on port 1.
        var service = CreateService(new Dictionary<string, string?>
        {
            ["SMTP:Enable"] = "true",
            ["SMTP:From"] = "noreply@example.com",
            ["SMTP:Host"] = "127.0.0.1",
            ["SMTP:Port"] = "1",
            ["SMTP:Username"] = "   ",
            ["SMTP:Password"] = "smtp-password"
        });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync("user@example.com", "Subject", "Body"));

        // Assert
        Assert.Contains("Failed to send e-mail to user@example.com", exception.Message);
    }
}
