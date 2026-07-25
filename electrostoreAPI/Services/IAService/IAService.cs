using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using ElectrostoreAPI.Services.FileService;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

namespace ElectrostoreAPI.Services.IAService;

public class IAService : IIAService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IFileService _fileService;
    private readonly IaCmdGrpc.IaCmdGrpcClient _iaGrpcClient;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IAService> _logger;

    public IAService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IFileService fileService, IaCmdGrpc.IaCmdGrpcClient iaGrpcClient, IKafkaProducerService kafkaProducer, IConfiguration configuration, ILogger<IAService> logger)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _fileService = fileService;
        _iaGrpcClient = iaGrpcClient;
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadIADto>> GetIA(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null)
    {
        _logger.LogDebug("GetIA: limit={Limit}, offset={Offset}, idResearchCount={IdResearchCount}", limit, offset, idResearch?.Count ?? 0);
        var query = _context.IA.AsQueryable();
        var filterResult = default(Expression<Func<IA, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(ia => idResearch.Contains(ia.id_ia));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<IA>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<IA>(sort);
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
        return new PaginatedResponseDto<ReadIADto>
        {
            data = _mapper.Map<List<ReadIADto>>(ia),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.IA.CountAsync(filterResult ?? (ia => true)),
                nextOffset = offset + limit,
                hasMore = await _context.IA.Skip(offset + limit).AnyAsync(filterResult ?? (ia => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadIADto> GetIAById(int id)
    {
        var ia = await _context.IA.FindAsync(id);
        if (ia is null)
        {
            _logger.LogWarning("GetIAById: IA {IaId} not found", id);
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        return _mapper.Map<ReadIADto>(ia);
    }

    public async Task<ReadIADto> CreateIA(CreateIADto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            _logger.LogWarning("CreateIA: client role {ClientRole} is not authorized to create IA", clientRole);
            throw new UnauthorizedAccessException("You are not authorized to create IA");
        }
        var newIA = _mapper.Map<IA>(iaDto);
        _context.IA.Add(newIA);
        await _context.SaveChangesAsync();
        _logger.LogInformation("IA {IaId} created", newIA.id_ia);
        return _mapper.Map<ReadIADto>(newIA);
    }

    public async Task<ReadIADto> UpdateIA(int id, UpdateIADto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            _logger.LogWarning("UpdateIA: client role {ClientRole} is not authorized to update IA {IaId}", clientRole, id);
            throw new UnauthorizedAccessException("You are not authorized to update IA");
        }
        var iaToUpdate = await _context.IA.FindAsync(id);
        if (iaToUpdate is null)
        {
            _logger.LogWarning("UpdateIA: IA {IaId} not found", id);
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        if (iaDto.nom_ia is not null)
        {
            iaToUpdate.nom_ia = iaDto.nom_ia;
        }
        if (iaDto.description_ia is not null)
        {
            iaToUpdate.description_ia = iaDto.description_ia;
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("IA {IaId} updated", id);
        return _mapper.Map<ReadIADto>(iaToUpdate);
    }

    public async Task DeleteIA(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            _logger.LogWarning("DeleteIA: client role {ClientRole} is not authorized to delete IA {IaId}", clientRole, id);
            throw new UnauthorizedAccessException("You are not authorized to delete IA");
        }
        var iaToDelete = await _context.IA.FindAsync(id);
        if (iaToDelete is null)
        {
            _logger.LogWarning("DeleteIA: IA {IaId} not found", id);
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        // remove model if exists
        _context.IA.Remove(iaToDelete);
        var iaMessage = new IaMessage
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
        _logger.LogInformation("IA {IaId} deleted", id);
    }

    public async Task<IAStatusDto> GetIATrainingStatusById(int id)
    {
        if (await _context.IA.FindAsync(id) == null)
        {
            _logger.LogWarning("GetIATrainingStatusById: IA {IaId} not found", id);
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        try
        {
            var reply = await _iaGrpcClient.GetStatusAsync(new StatusRequest { IdModel = id });
            return new IAStatusDto
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
            _logger.LogError(e, "GetIATrainingStatusById: error while fetching training status for IA {IaId}", id);
            return new IAStatusDto
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
            _logger.LogWarning("StartIATrainById: client role {ClientRole} is not authorized to train IA {IaId}", clientRole, id);
            throw new UnauthorizedAccessException("You are not authorized to train IA");
        }
        if (await _context.IA.FindAsync(id) == null)
        {
            _logger.LogWarning("StartIATrainById: IA {IaId} not found", id);
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        try
        {
            var iaMessage = new IaMessage
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
            _logger.LogInformation("IA {IaId} train requested", id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "StartIATrainById: error while requesting training for IA {IaId}", id);
            throw new InvalidOperationException("Error while training IA", e);
        }
    }

    public async Task<PredictionOutput> IADetectItem(int id, DetecDto detecDto)
    {
        var ia = await _context.IA.FindAsync(id);
        if (ia is null)
        {
            _logger.LogWarning("IADetectItem: IA {IaId} not found", id);
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        if (!ia.trained_ia)
        {
            _logger.LogWarning("IADetectItem: IA {IaId} is not trained", id);
            throw new InvalidOperationException("IA is not trained");
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
            _logger.LogError(e, "IADetectItem: error while detecting item using IA {IaId}", id);
            throw new InvalidOperationException("Error while detecting item", e);
        }
    }

    public async Task<bool> UpdateIaStatusAsync(int id, IAStatusDto iaStatus, int? requestedBy, CancellationToken cancellationToken)
    {
        var ia = await _context.IA.FindAsync(
            new object[] { id }, cancellationToken);

        if (ia is null)
        {
            _logger.LogWarning("UpdateIaStatusAsync: IA {IaId} not found for status update", id);
            return false;
        }

        // Update trained_ia flag based on the action
        if (iaStatus.Status == "training_completed")
        {
            ia.trained_ia = true;
            ia.date_training_ia = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("IA {IaId} training completed successfully", id);
        }
        else if (iaStatus.Status == "training_failed")
        {
            // trained_ia is left unchanged
            _logger.LogWarning("IA {IaId} training failed with message: {Message}", id, iaStatus.Message);
        }
        else if (iaStatus.Status == "training_started")
        {
            ia.trained_ia = false;
            ia.date_training_ia = null;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("IA {IaId} training started", id);
        }
        else
        {
            _logger.LogWarning("IA {IaId} received unknown status {Status}. No changes applied", id, iaStatus.Status);
            return false;
        }

        // Schedule a notification for terminal actions
        if (requestedBy != null && (iaStatus.Status == "training_completed" || iaStatus.Status == "training_failed"))
        {
            var requesterId = requestedBy.ToString();
            if (requesterId == null)
            {
                _logger.LogWarning("IA {IaId}: no valid requester id provided for notification, skipping notification", id);
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

                _logger.LogInformation("Notification for user {RequesterId} about IA {IaId} training {NotificationOutcome} has been published", requesterId, id, success ? "completion" : "failure");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateIaStatusAsync: error while publishing notification for IA {IaId} status update", id);
                // Even if notification fails, we consider the status update successful
            }
        }
        return true;
    }
}