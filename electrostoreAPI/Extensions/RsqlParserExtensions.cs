using System.Linq.Expressions;
using System.Reflection;
using electrostore.Dto;

namespace electrostore.Extensions;

public static class RsqlParserExtensions
{
    public static (Expression<Func<T, bool>>, List<FilterDto>?) ToFilterExpression<T>(List<FilterDto>? rsql)
    {
        if (rsql == null || rsql.Count == 0)
        {
            return (x => true, new List<FilterDto>());
        }
        ParameterExpression param = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;
        foreach (var condition in rsql)
        {
            var field = condition.Field;
            var searchType = condition.SearchType;
            var value = condition.Value;

            var propertyInfo = typeof(T).GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var fieldInfo = typeof(T).GetField(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propertyInfo == null && fieldInfo == null)
            {
                continue;
            }

            Expression left = Expression.PropertyOrField(param, field);
            Expression right = Expression.Constant(Convert.ChangeType(value, left.Type));

            Expression? binaryExpression = searchType switch
            {
                "eq" => Expression.Equal(left, right),
                "ne" => Expression.NotEqual(left, right),
                "gt" => Expression.GreaterThan(left, right),
                "lt" => Expression.LessThan(left, right),
                "ge" => Expression.GreaterThanOrEqual(left, right),
                "le" => Expression.LessThanOrEqual(left, right),
                "like" => Expression.Call(left, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, right),
                "null" => Expression.Equal(left, Expression.Constant(null)),
                "notnull" => Expression.NotEqual(left, Expression.Constant(null)),
                _ => null
            };

            if (binaryExpression != null)
            {
                combined = combined == null ? binaryExpression : Expression.AndAlso(combined, binaryExpression);
            }
        }
        if (combined == null)
        {
            return (x => true, new List<FilterDto>());
        }
        return (Expression.Lambda<Func<T, bool>>(combined, param), rsql);
    }

    public static (Expression<Func<T, object>>?, string) ToSortExpression<T>(SorterDto sort)
    {
        if (string.IsNullOrEmpty(sort.Field))
        {
            return (null, "asc");
        }
        var field = sort.Field;
        var direction = sort.Order?.ToLower() == "desc" ? "desc" : "asc";

        var propertyInfo = typeof(T).GetProperty(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        var fieldInfo = typeof(T).GetField(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (propertyInfo == null && fieldInfo == null)
        {
            return (null, "asc");
        }

        ParameterExpression param = Expression.Parameter(typeof(T), "x");
        Expression property = Expression.PropertyOrField(param, field);
        Expression converted = Expression.Convert(property, typeof(object));

        return (Expression.Lambda<Func<T, object>>(converted, param), direction);
    }
}