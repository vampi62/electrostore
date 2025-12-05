using Microsoft.AspNetCore.Http;
using Moq;
using electrostore.Validators;
using Xunit;

namespace electrostoreAPI.Tests.Validators;

public class FileSizeAttributeTests
{
    [Fact]
    public void IsValid_WithValidFileSize_ReturnsTrue()
    {
        // Arrange
        var maxSizeInMB = 5L;
        var attribute = new FileSizeAttribute(maxSizeInMB);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(2 * 1024 * 1024); // 2MB

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithFileSizeExceedsLimit_ReturnsFalse()
    {
        // Arrange
        var maxSizeInMB = 5L;
        var attribute = new FileSizeAttribute(maxSizeInMB);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(10 * 1024 * 1024); // 10MB

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithFileSizeEqualToLimit_ReturnsTrue()
    {
        // Arrange
        var maxSizeInMB = 5L;
        var attribute = new FileSizeAttribute(maxSizeInMB);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(5 * 1024 * 1024); // Exactly 5MB

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithEmptyFile_ReturnsFalse()
    {
        // Arrange
        var maxSizeInMB = 5L;
        var attribute = new FileSizeAttribute(maxSizeInMB);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNegativeFileSize_ReturnsFalse()
    {
        // Arrange
        var maxSizeInMB = 5L;
        var attribute = new FileSizeAttribute(maxSizeInMB);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(-1);

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNullValue_ReturnsFalse()
    {
        // Arrange
        var maxSizeInMB = 5L;
        var attribute = new FileSizeAttribute(maxSizeInMB);

        // Act
        var result = attribute.IsValid(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNonIFormFileValue_ReturnsFalse()
    {
        // Arrange
        var maxSizeInMB = 5L;
        var attribute = new FileSizeAttribute(maxSizeInMB);

        // Act
        var result = attribute.IsValid("not a file");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithZeroMaxSize_ReturnsFalse()
    {
        // Arrange
        var maxSizeInMB = 0L;
        var attribute = new FileSizeAttribute(maxSizeInMB);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1);

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithVerySmallFile_ReturnsTrue()
    {
        // Arrange
        var maxSizeInMB = 1L;
        var attribute = new FileSizeAttribute(maxSizeInMB);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1); // 1 byte

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(1L, 512 * 1024, true)]        // 0.5MB < 1MB
    [InlineData(1L, 1024 * 1024, true)]       // 1MB = 1MB
    [InlineData(1L, 2 * 1024 * 1024, false)]  // 2MB > 1MB
    [InlineData(10L, 5 * 1024 * 1024, true)]  // 5MB < 10MB
    [InlineData(10L, 15 * 1024 * 1024, false)] // 15MB > 10MB
    public void IsValid_WithVariousFileSizes_ReturnsExpectedResult(long maxSizeInMB, long fileSizeInBytes, bool expected)
    {
        // Arrange
        var attribute = new FileSizeAttribute(maxSizeInMB);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(fileSizeInBytes);

        // Act
        var result = attribute.IsValid(mockFile.Object);

        // Assert
        Assert.Equal(expected, result);
    }
}