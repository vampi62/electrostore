using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.CronJobService;

public class CronJobService : ICronJobService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<CronJobService> _logger;

    public CronJobService(IMapper mapper, ApplicationDbContext context, IKafkaProducerService kafkaProducer, ILogger<CronJobService> logger)
    {
        _mapper = mapper;
        _context = context;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadCronJobDto>> GetCronJobs(int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null)
    {
        _logger.LogDebug("GetCronJobs: limit={Limit}, offset={Offset}, idResearchCount={IdResearchCount}", limit, offset, idResearch?.Count ?? 0);
        var query = _context.CronJobs.AsQueryable();
        var filterResult = default(Expression<Func<CronJobs, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(c => idResearch.Contains(c.id_cronjob));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<CronJobs>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<CronJobs>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    query = query.OrderBy(c => c.id_cronjob);
                }
            }
            else
            {
                query = query.OrderBy(c => c.id_cronjob);
            }
        }
        var total = await _context.CronJobs.CountAsync(filterResult ?? (c => true));
        query = query.Skip(offset).Take(limit);
        var cronJobs = await query.ToListAsync();
        return new PaginatedResponseDto<ReadCronJobDto>
        {
            data = _mapper.Map<IEnumerable<ReadCronJobDto>>(cronJobs),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = total,
                nextOffset = offset + limit,
                hasMore = await _context.CronJobs.Skip(offset + limit).AnyAsync(filterResult ?? (c => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadCronJobDto> GetCronJobById(int id)
    {
        var cronJob = await _context.CronJobs.FindAsync(id);
        if (cronJob is null)
        {
            _logger.LogWarning("GetCronJobById: CronJob {CronJobId} not found", id);
            throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        }
        return _mapper.Map<ReadCronJobDto>(cronJob);
    }

    public async Task<ReadCronJobDto> CreateCronJob(CreateCronJobDto cronJobDto)
    {
        var newCronJob = _mapper.Map<CronJobs>(cronJobDto);
        _context.CronJobs.Add(newCronJob);
        await _context.SaveChangesAsync();
        var result = _mapper.Map<ReadCronJobDto>(newCronJob);
        var cronJobMessage = new CronJobMessage
        {
            action = "created",
            data = result
        };
        await _kafkaProducer.PublishAsync(
            "cronjob-events",
            newCronJob.id_cronjob.ToString(),
            JsonSerializer.Serialize(cronJobMessage)
        );
        _logger.LogInformation("CronJob {CronJobId} created", newCronJob.id_cronjob);
        return result;
    }

    public async Task<ReadCronJobDto> UpdateCronJob(int id, UpdateCronJobDto cronJobDto)
    {
        var cronJobToUpdate = await _context.CronJobs.FindAsync(id);
        if (cronJobToUpdate is null)
        {
            _logger.LogWarning("UpdateCronJob: CronJob {CronJobId} not found", id);
            throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        }

        if (cronJobDto.name_cronjob is not null)
        {
            cronJobToUpdate.name_cronjob = cronJobDto.name_cronjob;
        }
        if (cronJobDto.cron_expression is not null)
        {
            cronJobToUpdate.cron_expression = cronJobDto.cron_expression;
        }
        if (cronJobDto.action_cronjob is not null)
        {
            cronJobToUpdate.action_cronjob = cronJobDto.action_cronjob.Value;
        }
        if (cronJobDto.params_cronjob is not null)
        {
            cronJobToUpdate.params_cronjob = cronJobDto.params_cronjob;
        }
        if (cronJobDto.is_enabled is not null)
        {
            cronJobToUpdate.is_enabled = cronJobDto.is_enabled.Value;
        }
        if (cronJobDto.last_run_at is not null)
        {
            cronJobToUpdate.last_run_at = cronJobDto.last_run_at;
        }
        if (cronJobDto.next_run_at is not null)
        {
            cronJobToUpdate.next_run_at = cronJobDto.next_run_at;
        }

        await _context.SaveChangesAsync();
        var result = _mapper.Map<ReadCronJobDto>(cronJobToUpdate);
        var cronJobMessage = new CronJobMessage
        {
            action = "updated",
            data = result
        };
        await _kafkaProducer.PublishAsync(
            "cronjob-events",
            cronJobToUpdate.id_cronjob.ToString(),
            JsonSerializer.Serialize(cronJobMessage)
        );
        _logger.LogInformation("CronJob {CronJobId} updated", cronJobToUpdate.id_cronjob);
        return result;
    }

    public async Task DeleteCronJob(int id)
    {
        var cronJobToDelete = await _context.CronJobs.FindAsync(id);
        if (cronJobToDelete is null)
        {
            _logger.LogWarning("DeleteCronJob: CronJob {CronJobId} not found", id);
            throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        }
        var cronJobMessage = new CronJobMessage
        {
            action = "deleted",
            data = _mapper.Map<ReadCronJobDto>(cronJobToDelete)
        };
        _context.CronJobs.Remove(cronJobToDelete);
        await _context.SaveChangesAsync();
        await _kafkaProducer.PublishAsync(
            "cronjob-events",
            id.ToString(),
            JsonSerializer.Serialize(cronJobMessage)
         );
        _logger.LogInformation("CronJob {CronJobId} deleted", id);
    }

    public async Task<IEnumerable<ReadCronJobDto>> GetEnabledCronJobsAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("GetEnabledCronJobsAsync: fetching enabled cron jobs");
        var cronJobs = await _context.CronJobs.Where(c => c.is_enabled).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ReadCronJobDto>>(cronJobs);
    }

    public async Task UpdateCronJobRunAsync(int id, DateTime? lastRunAt, DateTime? nextRunAt, CancellationToken cancellationToken)
    {
        var cronJob = await _context.CronJobs.FindAsync(id, cancellationToken);
        if (cronJob is null)
        {
            _logger.LogWarning("UpdateCronJobRunAsync: CronJob {CronJobId} not found", id);
            throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        }
        if (lastRunAt.HasValue)
        {
            cronJob.last_run_at = lastRunAt.Value;
        }
        if (nextRunAt.HasValue)
        {
            cronJob.next_run_at = nextRunAt.Value;
        }
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("CronJob {CronJobId} run updated (lastRunAt={LastRunAt}, nextRunAt={NextRunAt})", id, lastRunAt, nextRunAt);
    }
}
