using System.Text.Json.Serialization;

namespace ElectrostoreAPI.Dto;

public record SorterDto
{
    public required string field { get; set; }
    public required string order { get; set; }
}

public record FilterDto
{
    public required string field { get; set; }
    // eq, ne, gt, lt, ge, le, like, null, notnull for a condition, "and" / "or" for a group
    public required string search_type { get; set; }
    public required string value { get; set; }
    // conditions combined by a group, a group has no field nor value
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<FilterDto>? children { get; set; }

    public static FilterDto And(IEnumerable<FilterDto> children) => Group("and", children);

    public static FilterDto Or(IEnumerable<FilterDto> children) => Group("or", children);

    private static FilterDto Group(string searchType, IEnumerable<FilterDto> children)
    {
        return new FilterDto { field = "", search_type = searchType, value = "", children = children.ToList() };
    }
}
