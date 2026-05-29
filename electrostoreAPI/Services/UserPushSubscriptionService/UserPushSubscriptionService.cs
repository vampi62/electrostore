using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace ElectrostoreAPI.Services.UserPushSubscriptionService;

public class UserPushSubscriptionService : IUserPushSubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UserPushSubscriptionService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedResponseDto<ReadUserPushSubscriptionDto>> GetPushSubscriptionsByUserId(int userId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var query = _context.UserPushSubscriptions.AsQueryable();
        var filterResult = default(Expression<Func<UserPushSubscriptions, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_user", SearchType = "eq", Value = userId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<UserPushSubscriptions>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<UserPushSubscriptions>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(s => s.created_at);
            }
        }
        else
        {
            query = query.OrderBy(s => s.created_at);
        }
        query = query.Skip(offset).Take(limit);
        var subscriptions = await query.ToListAsync();
        return new PaginatedResponseDto<ReadUserPushSubscriptionDto>
        {
            data = _mapper.Map<List<ReadUserPushSubscriptionDto>>(subscriptions),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.UserPushSubscriptions.CountAsync(filterResult ?? (us => us.id_user == userId)),
                nextOffset = offset + limit,
                hasMore = await _context.UserPushSubscriptions.Skip(offset + limit).AnyAsync(filterResult ?? (us => us.id_user == userId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadUserPushSubscriptionDto> GetPushSubscriptionById(int id, int? userId = null)
    {
        var subscription = await _context.UserPushSubscriptions.FindAsync(id) ?? throw new KeyNotFoundException($"Push subscription with id {id} not found");
        if (userId is not null && subscription.id_user != userId)
        {
            throw new KeyNotFoundException($"Push subscription with id {id} not found for user {userId}");
        }
        return _mapper.Map<ReadUserPushSubscriptionDto>(subscription);
    }

    public async Task<ReadUserPushSubscriptionDto> CreatePushSubscription(CreateUserPushSubscriptionDto dto)
    {
        if (!await _context.Users.AnyAsync(u => u.id_user == dto.id_user))
        {
            throw new KeyNotFoundException($"User with id {dto.id_user} not found");
        }

        // Upsert: if a subscription with the same endpoint already exists for this user, update it
        var existing = await _context.UserPushSubscriptions
            .FirstOrDefaultAsync(s => s.id_user == dto.id_user && s.endpoint == dto.endpoint);

        if (existing is not null)
        {
            existing.p256dh = dto.p256dh;
            existing.auth = dto.auth;
            existing.device_name = dto.device_name;
            await _context.SaveChangesAsync();
            return _mapper.Map<ReadUserPushSubscriptionDto>(existing);
        }

        var subscription = new UserPushSubscriptions
        {
            id_user = dto.id_user,
            endpoint = dto.endpoint,
            p256dh = dto.p256dh,
            auth = dto.auth,
            device_name = dto.device_name,
        };

        await _context.UserPushSubscriptions.AddAsync(subscription);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadUserPushSubscriptionDto>(subscription);
    }

    public async Task DeletePushSubscription(int id, int? userId = null)
    {
        var subscription = await _context.UserPushSubscriptions.FindAsync(id) ?? throw new KeyNotFoundException($"Push subscription with id {id} not found");
        if (userId is not null && subscription.id_user != userId)
        {
            throw new KeyNotFoundException($"Push subscription with id {id} not found for user {userId}");
        }
        _context.UserPushSubscriptions.Remove(subscription);
        await _context.SaveChangesAsync();
    }
}
