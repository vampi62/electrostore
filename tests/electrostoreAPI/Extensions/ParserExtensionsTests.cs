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

    [Fact]
    public void ParseFilter_ShouldReadInAsOrOfEqualities_WhenValuesAreInParentheses()
    {
        // Act
        var result = ParserExtensions.ParseFilter("tag=in=(a,b,c)");

        // Assert
        var group = Assert.Single(result);
        Assert.Equal("or", group.search_type);
        Assert.Equal(["a", "b", "c"], group.children!.Select(c => c.value));
        Assert.All(group.children!, c =>
        {
            Assert.Equal("tag", c.field);
            Assert.Equal("eq", c.search_type);
        });
    }

    [Fact]
    public void ParseFilter_ShouldReadInAsEquality_WhenThereIsASingleValue()
    {
        // Act
        var result = ParserExtensions.ParseFilter("tag=in=(a)");

        // Assert
        var filter = Assert.Single(result);
        Assert.Equal("eq", filter.search_type);
        Assert.Equal("a", filter.value);
    }

    [Fact]
    public void ParseFilter_ShouldParseOrGroup_BetweenInAndLike()
    {
        // Act
        var result = ParserExtensions.ParseFilter("(tag=in=(a,b),tag=like=res)");

        // Assert
        var group = Assert.Single(result);
        Assert.Equal("or", group.search_type);
        // the or of the "=in=" is merged in the group
        Assert.Equal(["eq", "eq", "like"], group.children!.Select(c => c.search_type));
        Assert.Equal(["a", "b", "res"], group.children!.Select(c => c.value));
    }

    [Fact]
    public void ParseFilter_ShouldGiveAndPrecedenceOverOr()
    {
        // Act
        var result = ParserExtensions.ParseFilter("a==1,b==2;c==3");

        // Assert
        var group = Assert.Single(result);
        Assert.Equal("or", group.search_type);
        Assert.Equal(2, group.children!.Count);
        Assert.Equal("eq", group.children![0].search_type);
        Assert.Equal("and", group.children![1].search_type);
    }

    [Fact]
    public void ParseFilter_ShouldKeepGroupAndOtherConditions_AsTopLevelAnd()
    {
        // Act
        var result = ParserExtensions.ParseFilter("x=ge=1;(a==1,b==2);y=le=5");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("x", result[0].field);
        Assert.Equal("or", result[1].search_type);
        Assert.Equal("y", result[2].field);
    }

    [Fact]
    public void ParseFilter_ShouldFlattenParenthesizedAnd()
    {
        // Act
        var result = ParserExtensions.ParseFilter("(a==1;b==2);c==3");

        // Assert
        Assert.Equal(["a", "b", "c"], result.Select(f => f.field));
    }

    [Fact]
    public void ParseFilter_ShouldKeepParentheses_OfAnAggregateField()
    {
        // Act
        var result = ParserExtensions.ParseFilter("SUM(ItemsBoxs.quantity_item_box)=ge=5");

        // Assert
        var filter = Assert.Single(result);
        Assert.Equal("SUM(ItemsBoxs.quantity_item_box)", filter.field);
        Assert.Equal("ge", filter.search_type);
        Assert.Equal("5", filter.value);
    }

    [Theory]
    [InlineData("name=like='a,b;(c)'", "a,b;(c)")]
    [InlineData("name==\"it's\"", "it's")]
    [InlineData("name=='it\\'s'", "it's")]
    [InlineData("name=like=res (1k)", "res (1k)")]
    public void ParseFilter_ShouldReadQuotedValues(string condition, string expectedValue)
    {
        // Act
        var result = ParserExtensions.ParseFilter(condition);

        // Assert
        var filter = Assert.Single(result);
        Assert.Equal("name", filter.field);
        Assert.Equal(expectedValue, filter.value);
    }

    [Fact]
    public void ParseFilter_ShouldReadQuotedValues_InAList()
    {
        // Act
        var result = ParserExtensions.ParseFilter("tag=in=('a,b', c)");

        // Assert
        var group = Assert.Single(result);
        Assert.Equal(["a,b", "c"], group.children!.Select(c => c.value));
    }

    [Theory]
    [InlineData("(a==1,garbage);b==2", 2)]
    [InlineData("tag=in=(a;b==2", 1)]
    [InlineData("a==1)garbage;b==2", 2)]
    [InlineData("()", 0)]
    public void ParseFilter_ShouldSkipMalformedGroups_ButKeepValidOnes(string condition, int expectedCount)
    {
        // Act
        var result = ParserExtensions.ParseFilter(condition);

        // Assert
        Assert.Equal(expectedCount, result.Count);
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
