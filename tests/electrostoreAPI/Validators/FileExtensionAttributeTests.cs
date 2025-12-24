using Microsoft.AspNetCore.Http;
using Moq;
using electrostore.Validators;
using electrostore.Dto;
using Xunit;

namespace electrostoreAPI.Tests.Validators;

public class FileTypeAttributeTests
{
    [Fact]
    public void IsValid_WithValidContentType_ReturnsTrue()
    {
        // Arrange
        var attribute = new FileTypeAttribute(nameof(Constants.AllowedDocumentMimeTypes));
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");
        mockFile.Setup(f => f.FileName).Returns("document.pdf");

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithInvalidContentType_ReturnsFalse()
    {
        // Arrange
        var attribute = new FileTypeAttribute(nameof(Constants.AllowedDocumentMimeTypes));
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("application/exe");
        mockFile.Setup(f => f.FileName).Returns("document.exe");

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var attribute = new FileTypeAttribute(nameof(Constants.AllowedDocumentMimeTypes));

        // Act
        var result = attribute.IsValid(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNonIFormFileValue_ReturnsFalse()
    {
        // Arrange
        var attribute = new FileTypeAttribute(nameof(Constants.AllowedDocumentMimeTypes));

        // Act
        var result = attribute.IsValid("not a file");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithInvalidPropertyName_ReturnsFalse()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new FileTypeAttribute("NonExistentProperty"));
    }

    [Fact]
    public void IsValid_WithCaseSensitiveContentType_ReturnsFalse()
    {
        // Arrange
        var attribute = new FileTypeAttribute(nameof(Constants.AllowedDocumentMimeTypes));
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns("APPLICATION/PDF");
        mockFile.Setup(f => f.FileName).Returns("document.pdf");

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("application/pdf", "document.pdf", true)]
    [InlineData("application/msword", "document.doc", true)]
    [InlineData("text/plain", "notes.txt", true)]
    [InlineData("application/octet-stream", "file.bin", false)]
    [InlineData("image/jpeg", "image.jpg", true)]
    [InlineData("image/png", "image.png", true)]
    [InlineData("video/mp4", "video.mp4", false)]
    [InlineData("", "empty.txt", false)]
    [InlineData("application/pdf", "", false)]
    public void IsValid_WithVariousContentTypes_ReturnsExpectedResult(string contentType, string fileName, bool expected)
    {
        // Arrange
        var attribute = new FileTypeAttribute(nameof(Constants.AllowedDocumentMimeTypes));
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.FileName).Returns(fileName);

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.Equal(expected, result);
    }
}