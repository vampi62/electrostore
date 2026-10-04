using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElectrostoreAPI.Services.CronJobService;

public class CronJobService : ICronJobService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly string KafkaCronJobTopic = "cronjob-events";

    public CronJobService(IMapper mapper, ApplicationDbContext context, IKafkaProducerService kafkaProducerService)
    {
        _mapper = mapper;
        _context = context;
        _kafkaProducerService = kafkaProducerService;
    }

    public async Task<PaginatedResponseDto<ReadCronJobDto>> GetCronJobs(int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null)
    {
        var query = _context.CronJobs.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(c => idResearch.Contains(c.id_cronjob));
            rsql = null;
            sort = null;
        }
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_cronjob", order = "asc" })
            .ToResponseAsync(cronJobs => _mapper.Map<IEnumerable<ReadCronJobDto>>(cronJobs));
    }

    public async Task<ReadCronJobDto> GetCronJobById(int id)
    {
        var cronJob = await _context.CronJobs.FindAsync(id)
            ?? throw new KeyNotFoundException($"CronJob with id '{id}' not found");
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
        await _kafkaProducerService.PublishAsync(
            KafkaCronJobTopic,
            newCronJob.id_cronjob.ToString(),
            JsonSerializer.Serialize(cronJobMessage)
        );
        return result;
    }

    public async Task<ReadCronJobDto> UpdateCronJob(int id, UpdateCronJobDto cronJobDto)
    {
        var cronJobToUpdate = await _context.CronJobs.FindAsync(id)
            ?? throw new KeyNotFoundException($"CronJob with id '{id}' not found");

        if (cronJobDto.name_cronjob is not null)
        {
            cronJobToUpdate.name_cronjob = cronJobDto.name_cronjob;
        }
        if (cronJobDto.cron_expression_cronjob is not null)
        {
            cronJobToUpdate.cron_expression_cronjob = cronJobDto.cron_expression_cronjob;
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
        await _kafkaProducerService.PublishAsync(
            KafkaCronJobTopic,
            cronJobToUpdate.id_cronjob.ToString(),
            JsonSerializer.Serialize(cronJobMessage)
        );
        return result;
    }

    public async Task DeleteCronJob(int id)
    {
        var cronJobToDelete = await _context.CronJobs.FindAsync(id)
            ?? throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        var cronJobMessage = new CronJobMessage
        {
            action = "deleted",
            data = _mapper.Map<ReadCronJobDto>(cronJobToDelete)
        };
        _context.CronJobs.Remove(cronJobToDelete);
        await _context.SaveChangesAsync();
        await _kafkaProducerService.PublishAsync(
            KafkaCronJobTopic,
            id.ToString(),
            JsonSerializer.Serialize(cronJobMessage)
         );
    }

    public async Task<IEnumerable<ReadCronJobDto>> GetEnabledCronJobsAsync(CancellationToken cancellationToken)
    {
        var cronJobs = await _context.CronJobs.Where(c => c.is_enabled).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ReadCronJobDto>>(cronJobs);
    }

    public async Task UpdateCronJobRunAsync(int id, DateTime? lastRunAt, DateTime? nextRunAt, CancellationToken cancellationToken)
    {
        var cronJob = await _context.CronJobs.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        if (lastRunAt.HasValue)
        {
            cronJob.last_run_at = lastRunAt.Value;
        }
        if (nextRunAt.HasValue)
        {
            cronJob.next_run_at = nextRunAt.Value;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCronJobStatusAsync(int id, CronJobStatus status, string? lastError, CancellationToken cancellationToken)
    {
        var cronJob = await _context.CronJobs.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        cronJob.status_cronjob = status;
        cronJob.last_error_cronjob = lastError;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ReadCronJobStatusDto> GetCronJobStatus(int id)
    {
        var cronJob = await _context.CronJobs.FindAsync(id)
            ?? throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        return new ReadCronJobStatusDto
        {
            id_cronjob = cronJob.id_cronjob,
            status_cronjob = cronJob.status_cronjob,
            last_error_cronjob = cronJob.last_error_cronjob,
            last_run_at = cronJob.last_run_at,
            next_run_at = cronJob.next_run_at
        };
    }

    public async Task ForceRunCronJob(int id)
    {
        var cronJob = await _context.CronJobs.FindAsync(id)
            ?? throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        if (!cronJob.is_enabled)
        {
            throw new InvalidOperationException($"CronJob with id '{id}' is disabled and cannot be force-run");
        }
        var cronJobMessage = new CronJobMessage
        {
            action = "force_run",
            data = _mapper.Map<ReadCronJobDto>(cronJob)
        };
        await _kafkaProducerService.PublishAsync(
            KafkaCronJobTopic,
            cronJob.id_cronjob.ToString(),
            JsonSerializer.Serialize(cronJobMessage)
        );
    }

    public async Task ForceStopCronJob(int id)
    {
        var cronJob = await _context.CronJobs.FindAsync(id)
            ?? throw new KeyNotFoundException($"CronJob with id '{id}' not found");
        var cronJobMessage = new CronJobMessage
        {
            action = "force_stop",
            data = _mapper.Map<ReadCronJobDto>(cronJob)
        };
        await _kafkaProducerService.PublishAsync(
            KafkaCronJobTopic,
            cronJob.id_cronjob.ToString(),
            JsonSerializer.Serialize(cronJobMessage)
        );
    }
}
