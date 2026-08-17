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
    private readonly IaCmdGrpc.IaCmdGrpcClient _aiGrpcClient;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIService> _logger;

    public AIService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IaCmdGrpc.IaCmdGrpcClient aiGrpcClient, IKafkaProducerService kafkaProducer, IConfiguration configuration, ILogger<AIService> logger)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _aiGrpcClient = aiGrpcClient;
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
            query = query.Where(ai => idResearch.Contains(ai.id_ia));
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
                    query = query.OrderBy(ai => ai.id_ia);
                }
            }
            else
            {
                query = query.OrderBy(ai => ai.id_ia);
            }
        }
        query = query.Skip(offset).Take(limit);
        var ai = await query.ToListAsync();
        return new PaginatedResponseDto<ReadAIDto>
        {
            data = _mapper.Map<List<ReadAIDto>>(ai),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.AI.CountAsync(filterResult ?? (ai => true)),
                nextOffset = offset + limit,
                hasMore = await _context.AI.Skip(offset + limit).AnyAsync(filterResult ?? (ai => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadAIDto> GetIAById(int id)
    {
        var ai = await _context.AI.FindAsync(id) ?? throw new KeyNotFoundException($"AI with id '{id}' not found");
        return _mapper.Map<ReadAIDto>(ai);
    }

    public async Task<ReadAIDto> CreateIA(CreateAIDto aiDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create AI");
        }
        var newIA = _mapper.Map<AI>(aiDto);
        _context.AI.Add(newIA);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadAIDto>(newIA);
    }

    public async Task<ReadAIDto> UpdateIA(int id, UpdateAIDto aiDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to update AI");
        }
        var aiToUpdate = await _context.AI.FindAsync(id) ?? throw new KeyNotFoundException($"AI with id '{id}' not found");
        if (aiDto.name_ia is not null)
        {
            aiToUpdate.name_ia = aiDto.name_ia;
        }
        if (aiDto.description_ia is not null)
        {
            aiToUpdate.description_ia = aiDto.description_ia;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadAIDto>(aiToUpdate);
    }

    public async Task DeleteIA(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete AI");
        }
        var aiToDelete = await _context.AI.FindAsync(id) ?? throw new KeyNotFoundException($"AI with id '{id}' not found");
        // remove model if exists
        _context.AI.Remove(aiToDelete);
        var aiMessage = new AiMessage
        {
            action = "ia_deleted",
            id_ia = id,
            requested_at = DateTime.UtcNow,
            requested_by = _sessionService.GetClientId()
        };
        await _kafkaProducer.PublishAsync(
            "ia-requests",
            id.ToString(),
            JsonSerializer.Serialize(aiMessage)
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
            var reply = await _aiGrpcClient.GetStatusAsync(new StatusRequest { IdModel = id });
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
            var aiMessage = new AiMessage
            {
                action = "train_requested",
                id_ia = id,
                requested_at = DateTime.UtcNow,
                requested_by = _sessionService.GetClientId()
            };
            await _kafkaProducer.PublishAsync(
                "ia-requests",
                id.ToString(),
                JsonSerializer.Serialize(aiMessage)
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
        var ai = await _context.AI.FindAsync(id) ?? throw new KeyNotFoundException($"AI with id '{id}' not found");
        if (!ai.trained_ia)
        {
            throw new InvalidOperationException("AI is not trained");
        }
        try
        {
            using var ms = new MemoryStream();
            await detecDto.img_file.OpenReadStream().CopyToAsync(ms);
            var imageBytes = ByteString.CopyFrom(ms.ToArray());

            var reply = await _aiGrpcClient.DetectAsync(new DetectRequest
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

    public async Task<bool> UpdateIaStatusAsync(int id, AIStatusDto aiStatus, int? requestedBy, CancellationToken cancellationToken)
    {
        var ai = await _context.AI.FindAsync(
            new object[] { id }, cancellationToken);

        if (ai is null)
        {
            _logger.LogWarning("AI with id {Id} not found for status update", id);
            return false;
        }

        // Update trained_ia flag based on the action
        if (aiStatus.Status == "training_completed")
        {
            ai.trained_ia = true;
            ai.date_training_ia = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI {Id}: training completed successfully", id);
        }
        else if (aiStatus.Status == "training_failed")
        {
            // trained_ia is left unchanged
            _logger.LogWarning("AI {Id}: training failed with message: {Message}", id, aiStatus.Message);
        }
        else if (aiStatus.Status == "training_started")
        {
            ai.trained_ia = false;
            ai.date_training_ia = null;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("AI {Id}: training started", id);
        }
        else
        {
            _logger.LogWarning("AI {Id}: received unknown status {Status}. No changes applied", id, aiStatus.Status);
            return false;
        }

        // Schedule a notification for terminal actions
        if (requestedBy != null && (aiStatus.Status == "training_completed" || aiStatus.Status == "training_failed"))
        {
            var requesterId = requestedBy.ToString();
            if (requesterId == null)
            {
                _logger.LogWarning("AI {Id}: no valid requester ID provided for notification, skipping notification", id);
                return true; // Status update succeeded, just no notification
            }
            try
            {
                bool success = aiStatus.Status == "training_completed";
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
                            ["aiId"]       = id.ToString(),
                            ["accuracy"]   = $"{aiStatus.Accuracy:P2}",
                            ["valAccuracy"] = $"{aiStatus.ValAccuracy:P2}",
                            ["loss"]       = $"{aiStatus.Loss:F4}",
                            ["valLoss"]    = $"{aiStatus.ValLoss:F4}",
                            ["epoch"]      = aiStatus.Epoch.ToString()
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
                            ["aiId"]    = id.ToString(),
                            ["message"] = aiStatus.Message ?? "Unknown error"
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