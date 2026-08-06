using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Validators;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using Xunit;

namespace ElectrostoreAPI.Tests.Validators;

public class FileTypeAttributeTests
{
    private const string ImageMimeTypesProperty = "AllowedImageMimeTypes";
    private const string DocumentMimeTypesProperty = "AllowedDocumentMimeTypes";

    private static Mock<IFormFile> BuildFormFile(string contentType, string fileName)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.FileName).Returns(fileName);
        return file;
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenPropertyNameDoesNotExist()
    {
        // Arrange & Act
        var exception = Record.Exception(() => new FileTypeAttribute("NotARealConstantsProperty"));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenMimeTypeAndExtensionAreAllowed()
    {
        // Arrange
        var attribute = new FileTypeAttribute(ImageMimeTypesProperty);
        var file = BuildFormFile("image/png", "picture.png");

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenMimeTypeIsNotAllowed()
    {
        // Arrange
        var attribute = new FileTypeAttribute(ImageMimeTypesProperty);
        var file = BuildFormFile("application/pdf", "document.pdf");

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenExtensionDoesNotMatchMimeType()
    {
        // Arrange
        var attribute = new FileTypeAttribute(ImageMimeTypesProperty);
        var file = BuildFormFile("image/png", "picture.exe");

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ShouldBeCaseInsensitive_ForExtension()
    {
        // Arrange
        var attribute = new FileTypeAttribute(ImageMimeTypesProperty);
        var file = BuildFormFile("image/png", "picture.PNG");

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_ForAllowedDocumentMimeType()
    {
        // Arrange
        var attribute = new FileTypeAttribute(DocumentMimeTypesProperty);
        var file = BuildFormFile("application/pdf", "document.pdf");

        // Act
        var result = attribute.IsValid(file.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenValueIsNull()
    {
        // Arrange
        var attribute = new FileTypeAttribute(ImageMimeTypesProperty);

        // Act
        var result = attribute.IsValid(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenValueIsNotAnIFormFile()
    {
        // Arrange
        var attribute = new FileTypeAttribute(ImageMimeTypesProperty);

        // Act
        var result = attribute.IsValid("not-a-file");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void FormatErrorMessage_ShouldIncludeAllowedMimeTypesAndExtensions()
    {
        // Arrange
        var attribute = new FileTypeAttribute(ImageMimeTypesProperty)
        {
            ErrorMessage = "{0} must be one of [{1}] with extension [{2}]"
        };

        // Act
        var message = attribute.FormatErrorMessage("file_field");

        // Assert
        Assert.Contains("file_field", message);
        Assert.Contains("image/png", message);
        Assert.Contains(".png", message);
    }
}
