using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

namespace ElectrostoreAPI.Services.AIService;

public class AIService : IAIService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IaCmdGrpc.IaCmdGrpcClient _iaGrpcClient;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIService> _logger;

    public AIService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IaCmdGrpc.IaCmdGrpcClient iaGrpcClient, IKafkaProducerService kafkaProducer, IConfiguration configuration, ILogger<AIService> logger)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _iaGrpcClient = iaGrpcClient;
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadAIDto>> GetIA(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null)
    {
        var query = _context.AI.AsQueryable();
        var filterResult = default(Expression<Func<AI, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(ia => idResearch.Contains(ia.id_ia));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<AI>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<AI>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_ia", Order = "asc" };
                    query = query.OrderBy(ia => ia.id_ia);
                }
            }
            else
            {
                query = query.OrderBy(ia => ia.id_ia);
            }
        }
        query = query.Skip(offset).Take(limit);
        var ia = await query.ToListAsync();
        return new PaginatedResponseDto<ReadAIDto>
        {
            data = _mapper.Map<List<ReadAIDto>>(ia),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.AI.CountAsync(filterResult ?? (ia => true)),
                nextOffset = offset + limit,
                hasMore = await _context.AI.Skip(offset + limit).AnyAsync(filterResult ?? (ia => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadAIDto> GetIAById(int id)
    {
        var ia = await _context.AI.FindAsync(id) ?? throw new KeyNotFoundException($"AI with id '{id}' not found");
        return _mapper.Map<ReadAIDto>(ia);
    }

    public async Task<ReadAIDto> CreateIA(CreateAIDto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create AI");
        }
        var newIA = _mapper.Map<AI>(iaDto);
        _context.AI.Add(newIA);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadAIDto>(newIA);
    }

    public async Task<ReadAIDto> UpdateIA(int id, UpdateAIDto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to update AI");
        }
        var iaToUpdate = await _context.AI.FindAsync(id) ?? throw new KeyNotFoundException($"AI with id '{id}' not found");
        if (iaDto.name_ia is not null)
        {
            iaToUpdate.name_ia = iaDto.name_ia;
        }
        if (iaDto.description_ia is not null)
        {
            iaToUpdate.description_ia = iaDto.description_ia;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadAIDto>(iaToUpdate);
    }

    public async Task DeleteIA(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete AI");
        }
        var iaToDelete = await _context.AI.FindAsync(id) ?? throw new KeyNotFoundException($"AI with id '{id}' not found");
        // remove model if exists
        _context.AI.Remove(iaToDelete);
        var iaMessage = new AiMessage
        {
            action = "ia_deleted",
            id_ia = id,
            requested_at = DateTime.UtcNow,
            requested_by = _sessionService.GetClientId()
        };
        await _kafkaProducer.PublishAsync(
            "ia-requests",
            id.ToString(),
            JsonSerializer.Serialize(iaMessage)
        );
        await _context.SaveChangesAsync();
    }

    public async Task<AIStatusDto> GetIATrainingStatusById(int id)
    {
        if (await _context.AI.FindAsync(id) == null)
        {
            throw new KeyNotFoundException($"AI with id '{id}' not found");
        }
        try
        {
            var reply = await _iaGrpcClient.GetStatusAsync(new StatusRequest { IdModel = id });
            return new AIStatusDto
            {
                Status      = reply.Status,
                Message     = reply.Message,
                Epoch       = reply.Epoch,
                Accuracy    = reply.Accuracy,
                ValAccuracy = reply.ValAccuracy,
                Loss        = reply.Loss,
                ValLoss     = reply.ValLoss
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while getting training status for AI {Id}", id);
            return new AIStatusDto
            {
                Status = "unknown",
                Message = "unknown",
                Epoch = 0,
                Accuracy = 0,
                ValAccuracy = 0,
                Loss = 0,
                ValLoss = 0
            };
        }
    }

    public async Task StartIATrainById(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to train AI");
        }
        if (await _context.AI.FindAsync(id) == null)
        {
            throw new KeyNotFoundException($"AI with id '{id}' not found");
        }
        try
        {
            var iaMessage = new AiMessage
            {
                action = "train_requested",
                id_ia = id,
                requested_at = DateTime.UtcNow,
                requested_by = _sessionService.GetClientId()
            };
            await _kafkaProducer.PublishAsync(
                "ia-requests",
                id.ToString(),
                JsonSerializer.Serialize(iaMessage)
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while starting training for AI {Id}", id);
            throw new InvalidOperationException("Error while training AI", e);
        }
    }

    public async Task<PredictionOutput> IADetectItem(int id, DetecDto detecDto)
    {
        var ia = await _context.AI.FindAsync(id) ?? throw new KeyNotFoundException($"AI with id '{id}' not found");
        if (!ia.trained_ia)
        {
            throw new InvalidOperationException("AI is not trained");
        }
        try
        {
            using var ms = new MemoryStream();
            await detecDto.img_file.OpenReadStream().CopyToAsync(ms);
            var imageBytes = ByteString.CopyFrom(ms.ToArray());

            var reply = await _iaGrpcClient.DetectAsync(new DetectRequest
            {
                IdModel   = id,
                ImageData = imageBytes
            });

            return new PredictionOutput
            {
                PredictedLabel = reply.PredictedClass,
                Score          = reply.Confidence
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while detecting item with AI {Id}", id);
            throw new InvalidOperationException("Error while detecting item", e);
        }
    }

    public async Task<bool> UpdateIaStatusAsync(int id, AIStatusDto iaStatus, int? requestedBy, CancellationToken cancellationToken)
    {
        var ia = await _context.AI.FindAsync(
            new object[] { id }, cancellationToken);

        if (ia is null)
        {
            _logger.LogWarning("AI with id {Id} not found for status update", id);
            return false;
        }

        // Update trained_ia flag based on the action
        if (iaStatus.Status == "training_completed")
        {
            ia.trained_ia = true;
            ia.date_training_ia = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI {Id}: training completed successfully", id);
        }
        else if (iaStatus.Status == "training_failed")
        {
            // trained_ia is left unchanged
            _logger.LogWarning("AI {Id}: training failed with message: {Message}", id, iaStatus.Message);
        }
        else if (iaStatus.Status == "training_started")
        {
            ia.trained_ia = false;
            ia.date_training_ia = null;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI {Id}: training started", id);
        }
        else
        {
            _logger.LogWarning("AI {Id}: received unknown status {Status}. No changes applied", id, iaStatus.Status);
            return false;
        }

        // Schedule a notification for terminal actions
        if (requestedBy != null && (iaStatus.Status == "training_completed" || iaStatus.Status == "training_failed"))
        {
            var requesterId = requestedBy.ToString();
            if (requesterId == null)
            {
                _logger.LogWarning("AI {Id}: no valid requester ID provided for notification, skipping notification", id);
                return true; // Status update succeeded, just no notification
            }
            try
            {
                bool success = iaStatus.Status == "training_completed";
                var lang = _configuration.GetValue<string>("AppLanguage") ?? "fr";

                NotificationMessage notification;
                if (success)
                {
                    notification = new NotificationMessage
                    {
                        Types = ["email"],
                        RecipientUserId = requestedBy,
                        TemplateId = "ia-training-completed",
                        Language = lang,
                        TemplateValues = new Dictionary<string, string>
                        {
                            ["iaId"]       = id.ToString(),
                            ["accuracy"]   = $"{iaStatus.Accuracy:P2}",
                            ["valAccuracy"] = $"{iaStatus.ValAccuracy:P2}",
                            ["loss"]       = $"{iaStatus.Loss:F4}",
                            ["valLoss"]    = $"{iaStatus.ValLoss:F4}",
                            ["epoch"]      = iaStatus.Epoch.ToString()
                        }
                    };
                }
                else
                {
                    notification = new NotificationMessage
                    {
                        Types = ["email"],
                        RecipientUserId = requestedBy,
                        TemplateId = "ia-training-failed",
                        Language = lang,
                        TemplateValues = new Dictionary<string, string>
                        {
                            ["iaId"]    = id.ToString(),
                            ["message"] = iaStatus.Message ?? "Unknown error"
                        }
                    };
                }

                await _kafkaProducer.PublishAsync(
                    "notification-requests",
                    requesterId + "-ia-training-status",
                    JsonSerializer.Serialize(notification),
                    cancellationToken);

                _logger.LogInformation("Notification for user {RequesterId} about AI {Id} training {Result} has been published",
                    requesterId, id, success ? "completion" : "failure");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while publishing notification for AI {Id} status update", id);
                // Even if notification fails, we consider the status update successful
            }
        }
        return true;
    }
}