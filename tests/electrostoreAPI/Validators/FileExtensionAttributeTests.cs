using Microsoft.AspNetCore.Http;
using Moq;
using electrostore.Validators;
using Xunit;

namespace electrostoreAPI.Tests.Validators;

public class FileExtensionAttributeTests
{
    [Fact]
    public void IsValid_WithValidContentType_ReturnsTrue()
    {
        // Arrange
        var allowedExtensions = new[] { "image/jpeg", "image/png", "application/pdf" };
        var attribute = new FileExtensionAttribute(allowedExtensions);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithInvalidContentType_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new[] { "image/jpeg", "image/png" };
        var attribute = new FileExtensionAttribute(allowedExtensions);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("application/exe");

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new[] { "image/jpeg", "image/png" };
        var attribute = new FileExtensionAttribute(allowedExtensions);

        // Act
        var result = attribute.IsValid(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNonIFormFileValue_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new[] { "image/jpeg", "image/png" };
        var attribute = new FileExtensionAttribute(allowedExtensions);

        // Act
        var result = attribute.IsValid("not a file");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithEmptyAllowedExtensions_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new string[0];
        var attribute = new FileExtensionAttribute(allowedExtensions);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNullContentType_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new[] { "image/jpeg", "image/png" };
        var attribute = new FileExtensionAttribute(allowedExtensions);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns((string?)null);

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithCaseSensitiveContentType_ReturnsFalse()
    {
        // Arrange
        var allowedExtensions = new[] { "image/jpeg" };
        var attribute = new FileExtensionAttribute(allowedExtensions);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("IMAGE/JPEG");

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("image/jpeg", true)]
    [InlineData("image/png", true)]
    [InlineData("application/pdf", true)]
    [InlineData("text/plain", false)]
    [InlineData("video/mp4", false)]
    [InlineData("", false)]
    public void IsValid_WithVariousContentTypes_ReturnsExpectedResult(string contentType, bool expected)
    {
        // Arrange
        var allowedExtensions = new[] { "image/jpeg", "image/png", "application/pdf" };
        var attribute = new FileExtensionAttribute(allowedExtensions);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns(contentType);

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.Equal(expected, result);
    }
}