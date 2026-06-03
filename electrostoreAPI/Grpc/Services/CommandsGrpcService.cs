using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.ConfigService;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElectrostoreAPI.Grpc.Services;

public class CommandsGrpcService : CommandsGrpc.CommandsGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommandsGrpcService> _logger;
    private readonly IKafkaProducerService _kafkaProducer;

    public CommandsGrpcService(
        ApplicationDbContext context,
        ILogger<CommandsGrpcService> logger,
        IKafkaProducerService kafkaProducer)
    {
        _context = context;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
    }

    public override async Task<UpdateCommandStatusReply> UpdateCommandStatus(
        UpdateCommandStatusRequest request, ServerCallContext context)
    {
        var command = await _context.Commands.FindAsync(
            new object[] { request.IdCommand }, context.CancellationToken);

        if (command is null)
        {
            _logger.LogWarning("UpdateCommandStatus: command #{Id} not found.", request.IdCommand);
            return new UpdateCommandStatusReply { Success = false };
        }

        command.status_command = request.StatusCommand;

        if (!string.IsNullOrWhiteSpace(request.DateLivraison)
            && DateTime.TryParse(request.DateLivraison, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var dateLivraison))
        {
            command.date_livraison_command = dateLivraison;
        }

        await _context.SaveChangesAsync(context.CancellationToken);
        return new UpdateCommandStatusReply { Success = true };
    }

    public override async Task<AddCommandHistoryReply> AddCommandHistory(
        AddCommandHistoryRequest request, ServerCallContext context)
    {
        if (!await _context.Commands.AnyAsync(c => c.id_command == request.IdCommand, context.CancellationToken))
        {
            _logger.LogWarning("AddCommandHistory: command #{Id} not found.", request.IdCommand);
            return new AddCommandHistoryReply { Success = false };
        }

        var eventAt = DateTime.TryParse(request.EventAt, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTime.UtcNow;

        var entry = new CommandsHistory
        {
            id_command             = request.IdCommand,
            status_command_history = request.Status,
            tracking_number        = string.IsNullOrWhiteSpace(request.TrackingNumber) ? null : request.TrackingNumber,
            carrier                = string.IsNullOrWhiteSpace(request.Carrier)        ? null : request.Carrier,
            tracking_event         = string.IsNullOrWhiteSpace(request.TrackingEvent)  ? null : request.TrackingEvent,
            event_at               = eventAt,
        };

        _context.CommandsHistory.Add(entry);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "AddCommandHistory: history entry #{HId} added for command #{CId} (status={Status}).",
            entry.id_command_history, request.IdCommand, request.Status);

        return new AddCommandHistoryReply { Success = true, IdCommandHistory = entry.id_command_history };
    }
}
