using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Validators;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using Xunit;

namespace ElectrostoreAPI.Tests.Validators;

public class FileSizeAttributeTests
{
    private const string ImageMaxSizeProperty = "MaxImageSizeMB";
    private const string DocumentMaxSizeProperty = "MaxDocumentSizeMB";

    private static Mock<IFormFile> BuildFormFile(long length)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(length);
        return file;
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenPropertyNameDoesNotExist()
    {
        // Arrange & Act
        var exception = Record.Exception(() => new FileSizeAttribute("NotARealConstantsProperty"));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenFileSizeWithinLimit()
    {
        // Arrange
        var attribute = new FileSizeAttribute(ImageMaxSizeProperty);
        var file = BuildFormFile(1024 * 1024); // 1 MB

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenFileSizeExactlyAtLimit()
    {
        // Arrange
        var attribute = new FileSizeAttribute(ImageMaxSizeProperty);
        var maxSize = Constants.MaxImageSizeMB * 1024L * 1024L;
        var file = BuildFormFile(maxSize);

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenFileSizeExceedsLimit()
    {
        // Arrange
        var attribute = new FileSizeAttribute(ImageMaxSizeProperty);
        var tooBig = (Constants.MaxImageSizeMB * 1024L * 1024L) + 1;
        var file = BuildFormFile(tooBig);

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenFileIsEmpty()
    {
        // Arrange
        var attribute = new FileSizeAttribute(ImageMaxSizeProperty);
        var file = BuildFormFile(0);

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ShouldUseCorrectLimit_ForDocumentProperty()
    {
        // Arrange
        var attribute = new FileSizeAttribute(DocumentMaxSizeProperty);
        var tooBig = (Constants.MaxDocumentSizeMB * 1024L * 1024L) + 1;
        var file = BuildFormFile(tooBig);

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenValueIsNull()
    {
        // Arrange
        var attribute = new FileSizeAttribute(ImageMaxSizeProperty);

        // Act
        var result = attribute.IsValid(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenValueIsNotAnIFormFile()
    {
        // Arrange
        var attribute = new FileSizeAttribute(ImageMaxSizeProperty);

        // Act
        var result = attribute.IsValid(12345);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FormatErrorMessage_ShouldIncludeMaxSizeInMB()
    {
        // Arrange
        var attribute = new FileSizeAttribute(ImageMaxSizeProperty)
        {
            ErrorMessage = "{0} must not exceed {1} MB"
        };

        // Act
        var message = attribute.FormatErrorMessage("file_field");

        // Assert
        Assert.Contains("file_field", message);
        Assert.Contains(Constants.MaxImageSizeMB.ToString(), message);
    }
}
