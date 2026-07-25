using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;


namespace ElectrostoreAPI.Services.UserPushSubscriptionService;

public class UserPushSubscriptionService : IUserPushSubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly ILogger<UserPushSubscriptionService> _logger;

    public UserPushSubscriptionService(ApplicationDbContext context, IMapper mapper, IConfiguration configuration, IKafkaProducerService kafkaProducerService, ILogger<UserPushSubscriptionService> logger)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
        _kafkaProducerService = kafkaProducerService;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadUserPushSubscriptionDto>> GetPushSubscriptionsByUserId(int userId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        _logger.LogDebug("GetPushSubscriptionsByUserId: userId={UserId}, limit={Limit}, offset={Offset}", userId, limit, offset);
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("GetPushSubscriptionsByUserId: User {UserId} not found", userId);
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
        var subscription = await _context.UserPushSubscriptions.FindAsync(id);
        if (subscription is null)
        {
            _logger.LogWarning("GetPushSubscriptionById: Push subscription {SubscriptionId} not found", id);
            throw new KeyNotFoundException($"Push subscription with id {id} not found");
        }
        if (userId is not null && subscription.id_user != userId)
        {
            _logger.LogWarning("GetPushSubscriptionById: Push subscription {SubscriptionId} not found for user {UserId}", id, userId);
            throw new KeyNotFoundException($"Push subscription with id {id} not found for user {userId}");
        }
        return _mapper.Map<ReadUserPushSubscriptionDto>(subscription);
    }

    public async Task<ReadUserPushSubscriptionDto> CreatePushSubscription(CreateUserPushSubscriptionDto dto)
    {
        if (!await _context.Users.AnyAsync(u => u.id_user == dto.id_user))
        {
            _logger.LogWarning("CreatePushSubscription: User {UserId} not found", dto.id_user);
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
            _logger.LogInformation("CreatePushSubscription: Push subscription {SubscriptionId} updated for user {UserId}", existing.id_push_subscription, existing.id_user);
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
        _logger.LogInformation("CreatePushSubscription: Push subscription {SubscriptionId} created for user {UserId}", subscription.id_push_subscription, subscription.id_user);
        return _mapper.Map<ReadUserPushSubscriptionDto>(subscription);
    }

    public async Task DeletePushSubscription(int id, int? userId = null)
    {
        var subscription = await _context.UserPushSubscriptions.FindAsync(id);
        if (subscription is null)
        {
            _logger.LogWarning("DeletePushSubscription: Push subscription {SubscriptionId} not found", id);
            throw new KeyNotFoundException($"Push subscription with id {id} not found");
        }
        if (userId is not null && subscription.id_user != userId)
        {
            _logger.LogWarning("DeletePushSubscription: Push subscription {SubscriptionId} not found for user {UserId}", id, userId);
            throw new KeyNotFoundException($"Push subscription with id {id} not found for user {userId}");
        }
        _context.UserPushSubscriptions.Remove(subscription);
        await _context.SaveChangesAsync();
        _logger.LogInformation("DeletePushSubscription: Push subscription {SubscriptionId} deleted", id);
    }

    public async Task<List<ReadUserPushSubscriptionDto>> GetPushSubscriptionsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("GetPushSubscriptionsByUserIdAsync: User {UserId} not found", userId);
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var subscriptions = await _context.UserPushSubscriptions
            .Where(s => s.id_user == userId)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<ReadUserPushSubscriptionDto>>(subscriptions);
    }

    public async Task SendTestPushNotification(int userId)
    {
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            _logger.LogWarning("SendTestPushNotification: User {UserId} not found", userId);
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var notification = new NotificationMessage
        {
            Types = ["webPush"],
            RecipientUserId = userId,
            Title = "Test notification",
            Body = "This is a test push notification",
            Language = _configuration.GetValue<string>("AppLanguage") ?? "fr"
        };
        await _kafkaProducerService.PublishAsync(
            "notification-requests",
            $"user-{userId}-push-test",
            JsonSerializer.Serialize(notification)
        );
        _logger.LogInformation("SendTestPushNotification: test push notification queued for user {UserId}", userId);
    }

    public async Task SendTestEmailNotification(int userId)
    {
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            _logger.LogWarning("SendTestEmailNotification: User {UserId} not found", userId);
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        var notification = new NotificationMessage
        {
            Types = ["email"],
            RecipientUserId = userId,
            Title = "Test notification",
            Body = "This is a test email notification",
            Language = _configuration.GetValue<string>("AppLanguage") ?? "fr"
        };
        await _kafkaProducerService.PublishAsync(
            "notification-requests",
            $"user-{userId}-email-test",
            JsonSerializer.Serialize(notification)
        );
        _logger.LogInformation("SendTestEmailNotification: test email notification queued for user {UserId}", userId);
    }
}
