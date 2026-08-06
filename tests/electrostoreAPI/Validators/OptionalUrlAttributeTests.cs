using ElectrostoreAPI.Validators;
using Xunit;

namespace ElectrostoreAPI.Tests.Validators;

public class OptionalUrlAttributeTests
{
    [Fact]
    public void IsValid_WithNullValue_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalUrlAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithEmptyString_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalUrlAttribute();

        // Act
        var result = attribute.IsValid("");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com")]
    [InlineData("https://example.com/path?query=1")]
    [InlineData("http://sub.example.com:8080/path")]
    public void IsValid_WithValidHttpOrHttpsUrl_ReturnsTrue(string url)
    {
        // Arrange
        var attribute = new OptionalUrlAttribute();

        // Act
        var result = attribute.IsValid(url);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    [InlineData("example.com")]
    [InlineData("file:///etc/passwd")]
    public void IsValid_WithInvalidOrNonHttpUrl_ReturnsFalse(string url)
    {
        // Arrange
        var attribute = new OptionalUrlAttribute();

        // Act
        var result = attribute.IsValid(url);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNonStringValue_ReturnsFalse()
    {
        // Arrange
        var attribute = new OptionalUrlAttribute();

        // Act
        var result = attribute.IsValid(42);

        // Assert
        Assert.False(result);
    }
}
