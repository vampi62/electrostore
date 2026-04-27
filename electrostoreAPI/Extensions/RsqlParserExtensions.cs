using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using electrostore.Dto;

namespace electrostore.Extensions;

public static class RsqlParserExtensions
{
    private static readonly Regex AggregateRegex =
        new(@"^(SUM|MAX|MIN|AVG|COUNT)\((.+)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsCollectionType(Type type)
    {
        return type.IsGenericType && 
               (type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                type.GetGenericTypeDefinition() == typeof(List<>) ||
                type.GetInterfaces().Any(i => i.IsGenericType && 
                    (i.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                     i.GetGenericTypeDefinition() == typeof(IEnumerable<>))));
    }

    private static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            return collectionType.GetGenericArguments()[0];
        }
        
        var enumerable = collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        
        return enumerable?.GetGenericArguments()[0];
    }

    private static Expression? BuildNestedPropertyAccess(Expression parameter, string field, out bool isCollection, out Type? collectionElementType, out string? collectionProperty)
    {
        isCollection = false;
        collectionElementType = null;
        collectionProperty = null;
        
        var parts = field.Split('.');
        Expression current = parameter;
        
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var propertyInfo = current.Type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var fieldInfo = current.Type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            
            if (propertyInfo == null && fieldInfo == null)
            {
                return null;
            }
            
            current = Expression.PropertyOrField(current, part);
            
            // if it's a collection and there are more parts, we need to use Any()
            if (IsCollectionType(current.Type) && i < parts.Length - 1)
            {
                isCollection = true;
                collectionElementType = GetCollectionElementType(current.Type);
                collectionProperty = string.Join(".", parts.Skip(i + 1));
                return current;
            }
        }
        
        return current;
    }

    private static (Expression? aggregateExpr, Type? resultType) BuildAggregateExpression(
        Expression parameter, string aggregateFunc, string innerField)
    {
        var parts = innerField.Split('.');
        Expression current = parameter;

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var propInfo = current.Type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var fieldInfoMember = current.Type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (propInfo == null && fieldInfoMember == null) return (null, null);

            var nextExpr = Expression.PropertyOrField(current, part);

            if (IsCollectionType(nextExpr.Type))
            {
                var elemType = GetCollectionElementType(nextExpr.Type);
                if (elemType == null) return (null, null);

                if (aggregateFunc.Equals("COUNT", StringComparison.OrdinalIgnoreCase))
                {
                    var countMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Count" && m.GetParameters().Length == 1)
                        .MakeGenericMethod(elemType);
                    return (Expression.Call(countMethod, nextExpr), typeof(int));
                }

                var remainingPath = string.Join(".", parts.Skip(i + 1));
                if (string.IsNullOrEmpty(remainingPath)) return (null, null);

                var innerParam = Expression.Parameter(elemType, "item");
                var innerProp = BuildNestedPropertyAccess(innerParam, remainingPath, out _, out _, out _);
                if (innerProp == null) return (null, null);

                var propType = innerProp.Type;
                var selector = Expression.Lambda(innerProp, innerParam);

                return BuildEnumerableAggregateCall(aggregateFunc, nextExpr, selector, elemType, propType);
            }

            current = nextExpr;
        }

        return (null, null);
    }

    private static (Expression? aggregateExpr, Type? resultType) BuildEnumerableAggregateCall(
        string aggregateFunc, Expression collection, LambdaExpression selector,
        Type elemType, Type propType)
    {
        MethodInfo? method = null;
        Type? resultType = null;

        if (aggregateFunc.Equals("SUM", StringComparison.OrdinalIgnoreCase))
        {
            method = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Sum" && m.IsGenericMethod && m.GetParameters().Length == 2)
                .FirstOrDefault(m =>
                {
                    var funcArgs = m.GetParameters()[1].ParameterType.GetGenericArguments();
                    return funcArgs.Length == 2 && funcArgs[1] == propType;
                })
                ?.MakeGenericMethod(elemType);
            resultType = method?.ReturnType;
        }
        else if (aggregateFunc.Equals("MAX", StringComparison.OrdinalIgnoreCase) ||
                 aggregateFunc.Equals("MIN", StringComparison.OrdinalIgnoreCase))
        {
            var methodName = aggregateFunc.Equals("MAX", StringComparison.OrdinalIgnoreCase) ? "Max" : "Min";
            method = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == methodName && m.IsGenericMethod &&
                            m.GetParameters().Length == 2 &&
                            m.GetGenericArguments().Length == 2)
                .FirstOrDefault()
                ?.MakeGenericMethod(elemType, propType);
            resultType = propType;
        }
        else if (aggregateFunc.Equals("AVG", StringComparison.OrdinalIgnoreCase))
        {
            method = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Average" && m.IsGenericMethod && m.GetParameters().Length == 2)
                .FirstOrDefault(m =>
                {
                    var funcArgs = m.GetParameters()[1].ParameterType.GetGenericArguments();
                    return funcArgs.Length == 2 && funcArgs[1] == propType;
                })
                ?.MakeGenericMethod(elemType);
            resultType = method?.ReturnType;
        }

        if (method == null || resultType == null) return (null, null);

        return (Expression.Call(method, collection, selector), resultType);
    }

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

            Expression? binaryExpression = null;

            // Check for aggregate function syntax: FUNC(field.path)
            var aggregateMatch = AggregateRegex.Match(field);
            if (aggregateMatch.Success)
            {
                var aggregateFunc = aggregateMatch.Groups[1].Value;
                var innerField = aggregateMatch.Groups[2].Value;

                var (aggregateExpr, resultType) = BuildAggregateExpression(param, aggregateFunc, innerField);
                if (aggregateExpr != null && resultType != null)
                {
                    object? convertedValue;
                    try
                    {
                        var underlyingType = Nullable.GetUnderlyingType(resultType) ?? resultType;
                        convertedValue = Convert.ChangeType(value, underlyingType);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    Expression right = Expression.Constant(convertedValue, resultType);
                    binaryExpression = searchType switch
                    {
                        "eq" => Expression.Equal(aggregateExpr, right),
                        "ne" => Expression.NotEqual(aggregateExpr, right),
                        "gt" => Expression.GreaterThan(aggregateExpr, right),
                        "lt" => Expression.LessThan(aggregateExpr, right),
                        "ge" => Expression.GreaterThanOrEqual(aggregateExpr, right),
                        "le" => Expression.LessThanOrEqual(aggregateExpr, right),
                        _ => null
                    };
                }
            }
            else
            {
                var left = BuildNestedPropertyAccess(param, field, out bool isCollection, out Type? collectionElementType, out string? collectionProperty);

                if (left == null)
                {
                    continue;
                }

                if (isCollection && collectionElementType != null && collectionProperty != null)
                {
                    // Expression for collection properties using Any()
                    var collectionParam = Expression.Parameter(collectionElementType, "item");
                    var itemProperty = BuildNestedPropertyAccess(collectionParam, collectionProperty, out _, out _, out _);

                    if (itemProperty == null)
                    {
                        continue;
                    }

                    object? convertedValue;
                    try
                    {
                        if (itemProperty.Type.IsEnum)
                        {
                            convertedValue = Enum.Parse(itemProperty.Type, value?.ToString() ?? "", true);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(value, itemProperty.Type);
                        }
                    }
                    catch (Exception)
                    {
                        // Skip this filter if conversion fails
                        continue;
                    }

                    Expression right = Expression.Constant(convertedValue);

                    Expression? itemCondition = searchType switch
                    {
                        "eq" => Expression.Equal(itemProperty, right),
                        "ne" => Expression.NotEqual(itemProperty, right),
                        "gt" => Expression.GreaterThan(itemProperty, right),
                        "lt" => Expression.LessThan(itemProperty, right),
                        "ge" => Expression.GreaterThanOrEqual(itemProperty, right),
                        "le" => Expression.LessThanOrEqual(itemProperty, right),
                        "like" => Expression.Call(itemProperty, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, right),
                        "null" => Expression.Equal(itemProperty, Expression.Constant(null)),
                        "notnull" => Expression.NotEqual(itemProperty, Expression.Constant(null)),
                        _ => null
                    };

                    if (itemCondition != null)
                    {
                        var lambda = Expression.Lambda(itemCondition, collectionParam);
                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(collectionElementType);

                        binaryExpression = Expression.Call(anyMethod, left, lambda);
                    }
                }
                else
                {
                    // Expression for non-collection properties
                    object? convertedValue;
                    try
                    {
                        if (left.Type.IsEnum)
                        {
                            convertedValue = Enum.Parse(left.Type, value?.ToString() ?? "", true);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(value, left.Type);
                        }
                    }
                    catch (Exception)
                    {
                        // Skip this filter if conversion fails
                        continue;
                    }

                    Expression right = Expression.Constant(convertedValue);

                    binaryExpression = searchType switch
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
                }
            }

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

        ParameterExpression param = Expression.Parameter(typeof(T), "x");

        // Check for aggregate function syntax: FUNC(field.path)
        var aggregateMatch = AggregateRegex.Match(field);
        if (aggregateMatch.Success)
        {
            var aggregateFunc = aggregateMatch.Groups[1].Value;
            var innerField = aggregateMatch.Groups[2].Value;

            var (aggregateExpr, _) = BuildAggregateExpression(param, aggregateFunc, innerField);
            if (aggregateExpr != null)
            {
                Expression convertedAgg = Expression.Convert(aggregateExpr, typeof(object));
                return (Expression.Lambda<Func<T, object>>(convertedAgg, param), direction);
            }
            return (null, "asc");
        }

        var property = BuildNestedPropertyAccess(param, field, out bool isCollection, out _, out _);

        if (property == null || isCollection)
        {
            return (null, "asc");
        }

        Expression converted = Expression.Convert(property, typeof(object));

        return (Expression.Lambda<Func<T, object>>(converted, param), direction);
    }
}