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
}
