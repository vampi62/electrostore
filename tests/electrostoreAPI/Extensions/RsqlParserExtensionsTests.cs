using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Xunit;

namespace ElectrostoreAPI.Tests.Extensions;

public class RsqlParserExtensionsTests
{
    private static Stores BuildStore(int id, string name, int xlength = 10, DateTime? mqttLastSeen = null, params int[] boxStarts)
    {
        var store = new Stores
        {
            id_store = id,
            nom_store = name,
            xlength_store = xlength,
            ylength_store = 10,
            mqtt_name_store = $"mqtt-{id}",
            mqtt_last_seen_store = mqttLastSeen
        };
        foreach (var start in boxStarts)
        {
            store.Boxs.Add(new Boxs { id_box = start, id_store = id, xstart_box = start, ystart_box = 0, xend_box = start + 1, yend_box = 1 });
        }
        return store;
    }

    private static List<FilterDto> Filter(string field, string searchType, string value)
    {
        return new List<FilterDto> { new FilterDto { Field = field, SearchType = searchType, Value = value } };
    }

    // --- ToFilterExpression: simple fields ---

    [Fact]
    public void ToFilterExpression_ShouldReturnMatchAll_WhenFilterListIsNull()
    {
        // Act
        var (expr, filters) = RsqlParserExtensions.ToFilterExpression<Stores>(null);

        // Assert
        Assert.True(expr.Compile()(BuildStore(1, "A")));
        Assert.Empty(filters!);
    }

    [Fact]
    public void ToFilterExpression_ShouldReturnMatchAll_WhenFilterListIsEmpty()
    {
        // Act
        var (expr, filters) = RsqlParserExtensions.ToFilterExpression<Stores>(new List<FilterDto>());

        // Assert
        Assert.True(expr.Compile()(BuildStore(1, "A")));
        Assert.Empty(filters!);
    }

    [Theory]
    [InlineData("eq", "Alpha", "Alpha", true)]
    [InlineData("eq", "Alpha", "Beta", false)]
    [InlineData("ne", "Alpha", "Beta", true)]
    [InlineData("ne", "Alpha", "Alpha", false)]
    public void ToFilterExpression_ShouldFilterByStringField_EqAndNe(string op, string storeName, string value, bool expected)
    {
        // Arrange
        var store = BuildStore(1, storeName);

        // Act
        var (expr, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("nom_store", op, value));

        // Assert
        Assert.Equal(expected, expr.Compile()(store));
    }

    [Fact]
    public void ToFilterExpression_ShouldFilterByStringField_Like()
    {
        // Arrange
        var store = BuildStore(1, "Warehouse North");

        // Act
        var (matching, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("nom_store", "like", "North"));
        var (nonMatching, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("nom_store", "like", "South"));

        // Assert
        Assert.True(matching.Compile()(store));
        Assert.False(nonMatching.Compile()(store));
    }

    [Theory]
    [InlineData("gt", 15, "10", true)]
    [InlineData("gt", 10, "10", false)]
    [InlineData("lt", 5, "10", true)]
    [InlineData("lt", 15, "10", false)]
    [InlineData("ge", 10, "10", true)]
    [InlineData("le", 10, "10", true)]
    public void ToFilterExpression_ShouldFilterByIntField_ComparisonOperators(string op, int xlength, string value, bool expected)
    {
        // Arrange
        var store = BuildStore(1, "A", xlength: xlength);

        // Act
        var (expr, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("xlength_store", op, value));

        // Assert
        Assert.Equal(expected, expr.Compile()(store));
    }

    [Fact]
    public void ToFilterExpression_ShouldFilterByNullableStringField_NullAndNotNull()
    {
        // Arrange
        var cameraWithoutUser = new Cameras { id_camera = 1, nom_camera = "A", url_camera = "http://a", user_camera = null };
        var cameraWithUser = new Cameras { id_camera = 2, nom_camera = "B", url_camera = "http://b", user_camera = "admin" };

        // Act
        var (isNull, _) = RsqlParserExtensions.ToFilterExpression<Cameras>(Filter("user_camera", "null", ""));
        var (isNotNull, _) = RsqlParserExtensions.ToFilterExpression<Cameras>(Filter("user_camera", "notnull", ""));

        // Assert
        Assert.True(isNull.Compile()(cameraWithoutUser));
        Assert.False(isNull.Compile()(cameraWithUser));
        Assert.False(isNotNull.Compile()(cameraWithoutUser));
        Assert.True(isNotNull.Compile()(cameraWithUser));
    }

    [Fact]
    public void ToFilterExpression_ShouldEffectivelyIgnoreNullFilter_OnNullableValueTypeField()
    {
        // A "null"/"notnull" filter still runs the "" value through Convert.ChangeType against the
        // field's underlying type before building the comparison. For a nullable value type (e.g.
        // DateTime?) that conversion throws, so the filter is silently dropped and every row matches
        // -- unlike the nullable reference type case above, where "" converts to "" without error.
        var storeWithoutMqtt = BuildStore(1, "A", mqttLastSeen: null);
        var storeWithMqtt = BuildStore(2, "B", mqttLastSeen: DateTime.UtcNow);

        // Act
        var (expr, filters) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("mqtt_last_seen_store", "null", ""));

        // Assert
        Assert.True(expr.Compile()(storeWithoutMqtt));
        Assert.True(expr.Compile()(storeWithMqtt));
        Assert.Empty(filters!);
    }

    [Fact]
    public void ToFilterExpression_ShouldSkipFilter_WhenFieldDoesNotExist()
    {
        // Arrange
        var store = BuildStore(1, "A");

        // Act
        var (expr, filters) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("does_not_exist", "eq", "x"));

        // Assert
        Assert.True(expr.Compile()(store));
        Assert.Empty(filters!);
    }

    [Fact]
    public void ToFilterExpression_ShouldSkipFilter_WhenValueCannotBeConverted()
    {
        // Arrange
        var store = BuildStore(1, "A", xlength: 10);

        // Act
        var (expr, filters) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("xlength_store", "eq", "not-a-number"));

        // Assert
        Assert.True(expr.Compile()(store));
        Assert.Empty(filters!);
    }

    [Fact]
    public void ToFilterExpression_ShouldCombineMultipleConditions_WithAnd()
    {
        // Arrange
        var matchingStore = BuildStore(1, "Alpha", xlength: 20);
        var wrongName = BuildStore(2, "Beta", xlength: 20);
        var wrongLength = BuildStore(3, "Alpha", xlength: 5);
        var filters = new List<FilterDto>
        {
            new FilterDto { Field = "nom_store", SearchType = "eq", Value = "Alpha" },
            new FilterDto { Field = "xlength_store", SearchType = "gt", Value = "10" }
        };

        // Act
        var (expr, returnedFilters) = RsqlParserExtensions.ToFilterExpression<Stores>(filters);

        // Assert
        Assert.True(expr.Compile()(matchingStore));
        Assert.False(expr.Compile()(wrongName));
        Assert.False(expr.Compile()(wrongLength));
        Assert.Equal(2, returnedFilters!.Count);
    }

    // --- ToFilterExpression: nested collection ---

    [Fact]
    public void ToFilterExpression_ShouldFilterByCollectionField_UsingAny()
    {
        // Arrange
        var storeWithMatchingBox = BuildStore(1, "A", boxStarts: new[] { 3, 7 });
        var storeWithoutMatchingBox = BuildStore(2, "B", boxStarts: new[] { 1, 2 });

        // Act
        var (expr, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("Boxs.xstart_box", "eq", "7"));

        // Assert
        Assert.True(expr.Compile()(storeWithMatchingBox));
        Assert.False(expr.Compile()(storeWithoutMatchingBox));
    }

    [Fact]
    public void ToFilterExpression_ShouldReturnFalse_ForCollectionField_WhenCollectionIsEmpty()
    {
        // Arrange
        var storeWithNoBoxes = BuildStore(1, "A");

        // Act
        var (expr, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("Boxs.xstart_box", "gt", "0"));

        // Assert
        Assert.False(expr.Compile()(storeWithNoBoxes));
    }

    // --- ToFilterExpression: aggregate functions ---

    [Fact]
    public void ToFilterExpression_ShouldFilterBySumAggregate()
    {
        // Arrange
        var store = BuildStore(1, "A", boxStarts: new[] { 3, 7 }); // sum = 10

        // Act
        var (matching, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("SUM(Boxs.xstart_box)", "eq", "10"));
        var (nonMatching, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("SUM(Boxs.xstart_box)", "eq", "5"));

        // Assert
        Assert.True(matching.Compile()(store));
        Assert.False(nonMatching.Compile()(store));
    }

    [Fact]
    public void ToFilterExpression_ShouldFilterByCountAggregate()
    {
        // Arrange
        var store = BuildStore(1, "A", boxStarts: new[] { 3, 7, 9 });

        // Act
        var (expr, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("COUNT(Boxs)", "eq", "3"));

        // Assert
        Assert.True(expr.Compile()(store));
    }

    [Fact]
    public void ToFilterExpression_ShouldFilterByMaxAndMinAggregate()
    {
        // Arrange
        var store = BuildStore(1, "A", boxStarts: new[] { 3, 7, 1 });

        // Act
        var (maxExpr, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("MAX(Boxs.xstart_box)", "eq", "7"));
        var (minExpr, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("MIN(Boxs.xstart_box)", "eq", "1"));

        // Assert
        Assert.True(maxExpr.Compile()(store));
        Assert.True(minExpr.Compile()(store));
    }

    [Fact]
    public void ToFilterExpression_ShouldFilterByAvgAggregate()
    {
        // Arrange
        var store = BuildStore(1, "A", boxStarts: new[] { 2, 4 }); // avg = 3

        // Act
        var (expr, _) = RsqlParserExtensions.ToFilterExpression<Stores>(Filter("AVG(Boxs.xstart_box)", "eq", "3"));

        // Assert
        Assert.True(expr.Compile()(store));
    }

    // --- ToSortExpression ---

    [Fact]
    public void ToSortExpression_ShouldReturnNull_WhenFieldIsEmpty()
    {
        // Act
        var (expr, direction) = RsqlParserExtensions.ToSortExpression<Stores>(new SorterDto { Field = "", Order = "asc" });

        // Assert
        Assert.Null(expr);
        Assert.Equal("asc", direction);
    }

    [Fact]
    public void ToSortExpression_ShouldReturnNull_WhenFieldDoesNotExist()
    {
        // Act
        var (expr, direction) = RsqlParserExtensions.ToSortExpression<Stores>(new SorterDto { Field = "does_not_exist", Order = "asc" });

        // Assert
        Assert.Null(expr);
        Assert.Equal("asc", direction);
    }

    [Fact]
    public void ToSortExpression_ShouldReturnNull_ForNestedCollectionField()
    {
        // Act
        var (expr, direction) = RsqlParserExtensions.ToSortExpression<Stores>(new SorterDto { Field = "Boxs.xstart_box", Order = "asc" });

        // Assert
        Assert.Null(expr);
        Assert.Equal("asc", direction);
    }

    [Fact]
    public void ToSortExpression_ShouldReturnExpression_ForDirectField()
    {
        // Arrange
        var store = BuildStore(1, "Alpha");

        // Act
        var (expr, direction) = RsqlParserExtensions.ToSortExpression<Stores>(new SorterDto { Field = "nom_store", Order = "desc" });

        // Assert
        Assert.NotNull(expr);
        Assert.Equal("desc", direction);
        Assert.Equal("Alpha", expr!.Compile()(store));
    }

    [Fact]
    public void ToSortExpression_ShouldDefaultToAscending_WhenOrderIsNotDesc()
    {
        // Act
        var (expr, direction) = RsqlParserExtensions.ToSortExpression<Stores>(new SorterDto { Field = "nom_store", Order = "sideways" });

        // Assert
        Assert.NotNull(expr);
        Assert.Equal("asc", direction);
    }

    [Fact]
    public void ToSortExpression_ShouldReturnExpression_ForAggregateField()
    {
        // Arrange
        var store = BuildStore(1, "A", boxStarts: new[] { 3, 7 });

        // Act
        var (expr, direction) = RsqlParserExtensions.ToSortExpression<Stores>(new SorterDto { Field = "SUM(Boxs.xstart_box)", Order = "asc" });

        // Assert
        Assert.NotNull(expr);
        Assert.Equal("asc", direction);
        Assert.Equal(10, expr!.Compile()(store));
    }
}
