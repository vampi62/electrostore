using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Extensions;

public static class ParserExtensions
{
    // the conditions to satisfy all together, see RsqlFilterParser for the syntax
    public static List<FilterDto> ParseFilter(string rsql)
    {
        return RsqlFilterParser.Parse(rsql);
    }

    public static SorterDto ParseSort(string sort)
    {
        if (string.IsNullOrEmpty(sort))
        {
            return new SorterDto { field = "", order = "asc" };
        }
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return new SorterDto { field = "", order = "asc" };
        }

        var field = parts[0].Trim();
        var order = string.Equals(parts[1].Trim(), "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        return new SorterDto { field = field, order = order };
    }
}