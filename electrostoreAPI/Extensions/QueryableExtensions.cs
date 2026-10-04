using ElectrostoreAPI.Dto;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Applies the RSQL filters and the requested sort (falling back to <paramref name="defaultSort"/>) on the query.
    /// Call ToResponseAsync (or ToProjectedResponseAsync for a projection) on the result to run the count + the page query.
    /// </summary>
    public static PagedQuery<T> ToPagedQuery<T>(this IQueryable<T> query, int limit, int offset,
        List<FilterDto>? filters, SorterDto? sorter, SorterDto defaultSort)
    {
        if (filters != null && filters.Count > 0)
        {
            var (filterExpr, appliedFilters) = RsqlParserExtensions.ToFilterExpression<T>(filters);
            query = query.Where(filterExpr);
            filters = appliedFilters;
        }

        SorterDto? appliedSort = null;
        var sortExpr = default(Expression<Func<T, object>>);
        var direction = "asc";
        if (!string.IsNullOrEmpty(sorter?.field))
        {
            (sortExpr, direction) = RsqlParserExtensions.ToSortExpression<T>(sorter);
            appliedSort = sorter;
        }
        if (sortExpr == null)
        {
            (sortExpr, direction) = RsqlParserExtensions.ToSortExpression<T>(defaultSort);
            // an invalid requested sort is reported as replaced by the default one, no requested sort is not reported
            appliedSort = string.IsNullOrEmpty(sorter?.field) ? null : defaultSort;
        }
        var sorted = direction == "desc" ? query.OrderByDescending(sortExpr!) : query.OrderBy(sortExpr!);
        return new PagedQuery<T>(query, sorted, limit, offset, filters, appliedSort);
    }
}

public sealed class PagedQuery<T>
{
    private readonly IQueryable<T> _filtered;
    private readonly IQueryable<T> _sorted;
    private readonly int _limit;
    private readonly int _offset;
    private readonly List<FilterDto>? _filters;
    private readonly SorterDto? _sort;

    internal PagedQuery(IQueryable<T> filtered, IQueryable<T> sorted, int limit, int offset, List<FilterDto>? filters, SorterDto? sort)
    {
        _filtered = filtered;
        _sorted = sorted;
        _limit = limit;
        _offset = offset;
        _filters = filters;
        _sort = sort;
    }

    public async Task<PaginatedResponseDto<TDto>> ToResponseAsync<TDto>(Func<IEnumerable<T>, IEnumerable<TDto>> map)
    {
        var total = await _filtered.CountAsync();
        var items = await _sorted.Skip(_offset).Take(_limit).ToListAsync();
        return BuildResponse(map(items), total);
    }

    /// <summary>
    /// The page is projected (e.g. counts / partial expand) before being materialized, the count is done on the entity query.
    /// </summary>
    public async Task<PaginatedResponseDto<TDto>> ToProjectedResponseAsync<TProjection, TDto>(
        Expression<Func<T, TProjection>> projection, Func<IEnumerable<TProjection>, IEnumerable<TDto>> map)
    {
        var total = await _filtered.CountAsync();
        var items = await _sorted.Skip(_offset).Take(_limit).Select(projection).ToListAsync();
        return BuildResponse(map(items), total);
    }

    private PaginatedResponseDto<TDto> BuildResponse<TDto>(IEnumerable<TDto> data, int total)
    {
        return new PaginatedResponseDto<TDto>
        {
            data = data,
            pagination = new PaginationDto
            {
                limit = _limit,
                offset = _offset,
                total = total,
                next_offset = _offset + _limit,
                has_more = _offset + _limit < total
            },
            filters = _filters,
            sort = _sort != null ? [_sort] : null
        };
    }
}
