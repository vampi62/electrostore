using electrostore.Validators;
using Xunit;

namespace electrostoreAPI.Tests.Validators;

public class OptionalNotEmptyAttributeTests
{
    [Fact]
    public void IsValid_WithNullValue_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid(null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithValidString_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid("valid string");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithWhitespaceOnlyString_ReturnsFalse()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid("   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithTabsAndSpaces_ReturnsFalse()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid("\t\n\r   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithSingleSpace_ReturnsFalse()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid(" ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithNonStringValue_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid(42);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithBooleanValue_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid(true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithDateTimeValue_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid(DateTime.Now);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithObjectValue_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();
        var testObject = new { Name = "Test" };

        // Act
        var result = attribute.IsValid(testObject);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("valid text", true)]
    [InlineData("a", true)]
    [InlineData("  valid with spaces  ", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("  ", false)]
    [InlineData("\t", false)]
    [InlineData("\n", false)]
    [InlineData("\r", false)]
    [InlineData("\r\n", false)]
    [InlineData("   \t\n\r   ", false)]
    public void IsValid_WithVariousStringValues_ReturnsExpectedResult(string? value, bool expected)
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid(value);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValid_WithStringContainingOnlyNewlines_ReturnsFalse()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid("\n\n\n");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WithValidStringAfterWhitespace_ReturnsTrue()
    {
        // Arrange
        var attribute = new OptionalNotEmptyAttribute();

        // Act
        var result = attribute.IsValid("  hello  ");

        // Assert
        Assert.True(result);
    }
}