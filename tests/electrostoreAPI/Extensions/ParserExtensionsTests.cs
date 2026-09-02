using ElectrostoreAPI.Extensions;
using Xunit;

namespace ElectrostoreAPI.Tests.Extensions;

public class ParserExtensionsTests
{
    // --- ParseFilter ---

    [Fact]
    public void ParseFilter_ShouldReturnEmptyList_WhenInputIsEmpty()
    {
        // Act
        var result = ParserExtensions.ParseFilter("");

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("field==value", "eq")]
    [InlineData("field!=value", "ne")]
    [InlineData("field=gt=5", "gt")]
    [InlineData("field=lt=5", "lt")]
    [InlineData("field=ge=5", "ge")]
    [InlineData("field=le=5", "le")]
    [InlineData("field=like=value", "like")]
    public void ParseFilter_ShouldParseSingleCondition_ForEachOperator(string condition, string expectedSearchType)
    {
        // Act
        var result = ParserExtensions.ParseFilter(condition);

        // Assert
        var filter = Assert.Single(result);
        Assert.Equal("field", filter.field);
        Assert.Equal(expectedSearchType, filter.search_type);
    }

    [Fact]
    public void ParseFilter_ShouldParseNullOperator_WithEmptyValue()
    {
        // Act
        var result = ParserExtensions.ParseFilter("field=null=");

        // Assert
        var filter = Assert.Single(result);
        Assert.Equal("field", filter.field);
        Assert.Equal("null", filter.search_type);
        Assert.Equal("", filter.value);
    }

    [Fact]
    public void ParseFilter_ShouldParseMultipleConditions_SeparatedBySemicolon()
    {
        // Act
        var result = ParserExtensions.ParseFilter("a==1;b=gt=2");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].field);
        Assert.Equal("1", result[0].value);
        Assert.Equal("eq", result[0].search_type);
        Assert.Equal("b", result[1].field);
        Assert.Equal("2", result[1].value);
        Assert.Equal("gt", result[1].search_type);
    }

    [Fact]
    public void ParseFilter_ShouldIgnoreEmptyConditions_BetweenSemicolons()
    {
        // Act
        var result = ParserExtensions.ParseFilter("a==1;;b==2;");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ParseFilter_ShouldSkipCondition_WhenNoOperatorPresent()
    {
        // Act
        var result = ParserExtensions.ParseFilter("fieldwithoutoperator");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseFilter_ShouldSkipMalformedCondition_ButKeepValidOnes()
    {
        // Act
        var result = ParserExtensions.ParseFilter("garbage;a==1");

        // Assert
        var filter = Assert.Single(result);
        Assert.Equal("a", filter.field);
    }

    // --- ParseSort ---

    [Fact]
    public void ParseSort_ShouldReturnDefault_WhenInputIsNull()
    {
        // Act
        var result = ParserExtensions.ParseSort(null!);

        // Assert
        Assert.Equal("", result.field);
        Assert.Equal("asc", result.order);
    }

    [Fact]
    public void ParseSort_ShouldReturnDefault_WhenInputIsEmpty()
    {
        // Act
        var result = ParserExtensions.ParseSort("");

        // Assert
        Assert.Equal("", result.field);
        Assert.Equal("asc", result.order);
    }

    [Fact]
    public void ParseSort_ShouldReturnDefault_WhenMissingComma()
    {
        // Act
        var result = ParserExtensions.ParseSort("fieldonly");

        // Assert
        Assert.Equal("", result.field);
        Assert.Equal("asc", result.order);
    }

    [Theory]
    [InlineData("field,asc", "field", "asc")]
    [InlineData("field,desc", "field", "desc")]
    [InlineData("field,DESC", "field", "desc")]
    [InlineData("field,unknown", "field", "asc")]
    public void ParseSort_ShouldParseFieldAndOrder(string input, string expectedField, string expectedOrder)
    {
        // Act
        var result = ParserExtensions.ParseSort(input);

        // Assert
        Assert.Equal(expectedField, result.field);
        Assert.Equal(expectedOrder, result.order);
    }

    [Fact]
    public void ParseSort_ShouldTrimWhitespace_AroundFieldAndOrder()
    {
        // Act
        var result = ParserExtensions.ParseSort(" field , desc ");

        // Assert
        Assert.Equal("field", result.field);
        Assert.Equal("desc", result.order);
    }
}
