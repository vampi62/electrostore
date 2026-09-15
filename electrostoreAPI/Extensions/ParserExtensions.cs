using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Extensions;

public static class ParserExtensions
{
    public static List<FilterDto> ParseFilter(string rsql)
    {
        var conditions = rsql.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var filters = new List<FilterDto>();
        foreach (var condition in conditions)
        {
            var parts = condition.Split(["==", "!=", "=gt=", "=lt=", "=ge=", "=le=", "=like=", "=null=", "!=null=", "=any="], StringSplitOptions.None);
            if (parts.Length != 2) continue;

            var field = parts[0];
            var value = parts[1];

            var searchType = GetSearchType(condition);

            if (searchType != null)
            {
                filters.Add(new FilterDto { field = field, value = value, search_type = searchType });
            }
        }
        return filters;
    }

    private static string? GetSearchType(string condition)
    {
        if (condition.Contains("==")) return "eq";
        if (condition.Contains("!=")) return "ne";
        if (condition.Contains("=gt=")) return "gt";
        if (condition.Contains("=lt=")) return "lt";
        if (condition.Contains("=ge=")) return "ge";
        if (condition.Contains("=le=")) return "le";
        if (condition.Contains("=like=")) return "like";
        if (condition.Contains("=null=")) return "null";
        if (condition.Contains("!=null=")) return "notnull";
        if (condition.Contains("=any=")) return "any";
        return null;
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