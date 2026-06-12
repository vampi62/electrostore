using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class CommandsGrpcService : CommandsGrpc.CommandsGrpcBase
{
    private readonly ILogger<CommandsGrpcService> _logger;

    public CommandsGrpcService(
        ILogger<CommandsGrpcService> logger)
    {
        _logger = logger;
    }

    public override async Task<UpdateCommandStatusReply> UpdateCommandStatus(
        UpdateCommandStatusRequest request, ServerCallContext context)
    {
        return new UpdateCommandStatusReply { Success = true };
    }

    public override async Task<AddCommandHistoryReply> AddCommandHistory(
        AddCommandHistoryRequest request, ServerCallContext context)
    {
        return new AddCommandHistoryReply { Success = true, IdCommandHistory = 0 };
    }
}
