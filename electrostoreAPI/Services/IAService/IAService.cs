using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Grpc;
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

    public IAService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IFileService fileService, IaCmdGrpc.IaCmdGrpcClient iaGrpcClient, IKafkaProducerService kafkaProducer)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _fileService = fileService;
        _iaGrpcClient = iaGrpcClient;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<PaginatedResponseDto<ReadIADto>> GetIA(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<int>? idResearch = null)
    {
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
        var ia = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id '{id}' not found");
        return _mapper.Map<ReadIADto>(ia);
    }

    public async Task<ReadIADto> CreateIA(CreateIADto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create IA");
        }
        var newIA = _mapper.Map<IA>(iaDto);
        _context.IA.Add(newIA);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadIADto>(newIA);
    }

    public async Task<ReadIADto> UpdateIA(int id, UpdateIADto iaDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to update IA");
        }
        var iaToUpdate = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id '{id}' not found");
        if (iaDto.nom_ia is not null)
        {
            iaToUpdate.nom_ia = iaDto.nom_ia;
        }
        if (iaDto.description_ia is not null)
        {
            iaToUpdate.description_ia = iaDto.description_ia;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadIADto>(iaToUpdate);
    }

    public async Task DeleteIA(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete IA");
        }
        var iaToDelete = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id '{id}' not found");
        // remove model if exists
        _context.IA.Remove(iaToDelete);
        await _kafkaProducer.PublishAsync(
            "ia-requests",
            id.ToString(),
            JsonSerializer.Serialize(new { action = "ia_deleted", id_ia = id, deleted_at = DateTime.UtcNow, deleted_by = _sessionService.GetClientId() })
        );
        await _context.SaveChangesAsync();
    }

    public async Task<IAStatusDto> GetIATrainingStatusById(int id)
    {
        if (await _context.IA.FindAsync(id) == null)
        {
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
            Console.WriteLine(e);
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
            throw new UnauthorizedAccessException("You are not authorized to train IA");
        }
        if (await _context.IA.FindAsync(id) == null)
        {
            throw new KeyNotFoundException($"IA with id '{id}' not found");
        }
        try
        {
            await _kafkaProducer.PublishAsync(
                "ia-requests",
                id.ToString(),
                JsonSerializer.Serialize(new { action = "train_requested", id_ia = id, requested_at = DateTime.UtcNow, requested_by = _sessionService.GetClientId() })
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new InvalidOperationException("Error while training IA", e);
        }
    }

    public async Task<PredictionOutput> IADetectItem(int id, DetecDto detecDto)
    {
        var ia = await _context.IA.FindAsync(id) ?? throw new KeyNotFoundException($"IA with id '{id}' not found");
        if (!ia.trained_ia)
        {
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
            Console.WriteLine(e);
            throw new InvalidOperationException("Error while detecting item", e);
        }
    }
}