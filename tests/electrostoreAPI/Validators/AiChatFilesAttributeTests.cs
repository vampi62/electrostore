using ElectrostoreAPI.Constants;
using ElectrostoreAPI.Validators;
using Microsoft.AspNetCore.Http;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ElectrostoreAPI.Tests.Validators;

public class AiChatFilesAttributeTests
{
    private static IFormFile BuildFile(string fileName, string contentType, long length)
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
        return file.Object;
    }

    private static ValidationResult? Validate(object? value) =>
        new AiChatFilesAttribute().GetValidationResult(value, new ValidationContext(new object()) { DisplayName = "files" });

    [Fact]
    public void Classify_ShouldDecideByExtension_ForATextFile()
    {
        // Windows gives a csv the mime type of Excel
        Assert.Equal(AiChatFileKind.Text, AiChatFileClassifier.Classify(BuildFile("bom.CSV", "application/vnd.ms-excel", 10)));
        Assert.Equal(AiChatFileKind.Text, AiChatFileClassifier.Classify(BuildFile("run.log", "", 10)));
    }

    [Fact]
    public void Classify_ShouldAcceptAnImage_WhenMimeTypeAndExtensionAreAllowed()
    {
        Assert.Equal(AiChatFileKind.Image, AiChatFileClassifier.Classify(BuildFile("board.png", "image/png", 10)));
    }

    [Theory]
    [InlineData("board.png", "application/octet-stream")]
    [InlineData("program.exe", "image/png")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("noextension", "text/plain")]
    public void Classify_ShouldRefuse_WhenTypeIsNotAllowed(string fileName, string contentType)
    {
        Assert.Equal(AiChatFileKind.Unsupported, AiChatFileClassifier.Classify(BuildFile(fileName, contentType, 10)));
    }

    [Fact]
    public void IsValid_ShouldBeSuccess_WhenThereIsNoFile()
    {
        Assert.Equal(ValidationResult.Success, Validate(null));
        Assert.Equal(ValidationResult.Success, Validate(new List<IFormFile>()));
    }

    [Fact]
    public void IsValid_ShouldBeSuccess_WhenFilesAreAllowed()
    {
        var files = new List<IFormFile> { BuildFile("board.png", "image/png", 1024), BuildFile("bom.csv", "text/csv", 1024) };

        Assert.Equal(ValidationResult.Success, Validate(files));
    }

    [Fact]
    public void IsValid_ShouldFail_WhenThereAreTooManyFiles()
    {
        var files = Enumerable.Range(0, FieldLengths.MaxAiChatFileCount + 1).Select(i => BuildFile($"{i}.txt", "text/plain", 10)).ToList();

        Assert.NotEqual(ValidationResult.Success, Validate(files));
    }

    [Fact]
    public void IsValid_ShouldFail_WhenATypeIsNotAllowed()
    {
        var result = Validate(new List<IFormFile> { BuildFile("program.exe", "application/octet-stream", 10) });

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("program.exe", result!.ErrorMessage);
    }

    [Fact]
    public void IsValid_ShouldFail_WhenAFileIsEmpty()
    {
        Assert.NotEqual(ValidationResult.Success, Validate(new List<IFormFile> { BuildFile("empty.txt", "text/plain", 0) }));
    }

    [Fact]
    public void IsValid_ShouldFail_WhenAnImageIsTooBig()
    {
        var tooBig = FieldLengths.MaxImageSizeMB * 1024L * 1024L + 1;

        Assert.NotEqual(ValidationResult.Success, Validate(new List<IFormFile> { BuildFile("big.png", "image/png", tooBig) }));
    }

    [Fact]
    public void IsValid_ShouldFail_WhenATextFileIsTooBig()
    {
        var tooBig = FieldLengths.MaxDocumentSizeMB * 1024L * 1024L + 1;

        Assert.NotEqual(ValidationResult.Success, Validate(new List<IFormFile> { BuildFile("big.txt", "text/plain", tooBig) }));
    }
}
