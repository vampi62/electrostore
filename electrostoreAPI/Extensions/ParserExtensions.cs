using System.Linq.Expressions;
using System.Reflection;
using electrostore.Dto;

namespace electrostore.Extensions;

public static class ParserExtensions
{
    public static List<FilterDto> ParseFilter(string rsql)
    {
        var conditions = rsql.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var filters = new List<FilterDto>();
        foreach (var condition in conditions)
        {
            var parts = condition.Split(["==", "!=", "=gt=", "=lt=", "=ge=", "=le=", "=like=", "=null=", "!=null="], StringSplitOptions.None);
            if (parts.Length != 2) continue;

            var field = parts[0];
            var value = parts[1];

            var searchType = condition.Contains("==") ? "eq" :
                             condition.Contains("!=") ? "ne" :
                             condition.Contains("=gt=") ? "gt" :
                             condition.Contains("=lt=") ? "lt" :
                             condition.Contains("=ge=") ? "ge" :
                             condition.Contains("=le=") ? "le" :
                             condition.Contains("=like=") ? "like" :
                             condition.Contains("=null=") ? "null" :
                             condition.Contains("!=null=") ? "notnull" :
                             null;

            if (searchType != null)
            {
                filters.Add(new FilterDto { Field = field, Value = value, SearchType = searchType });
            }
        }
        return filters;
    }

    public static SorterDto ParseSort(string sort)
    {
        if (string.IsNullOrEmpty(sort))
        {
            return new SorterDto { Field = "Id", Order = "asc" };
        }
        var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return new SorterDto { Field = "Id", Order = "asc" };
        }

        var field = parts[0].Trim();
        var order = parts[1].Trim().ToLower() == "desc" ? "desc" : "asc";

        return new SorterDto { Field = field, Order = order };
    }
}