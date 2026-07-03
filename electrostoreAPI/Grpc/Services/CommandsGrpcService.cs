using ElectrostoreAPI.Services.CommandService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class CommandsGrpcService : CommandsGrpc.CommandsGrpcBase
{
    private readonly ICommandService                 _commandService;
    private readonly ILogger<CommandsGrpcService>   _logger;

    public CommandsGrpcService(
        ICommandService commandService,
        ILogger<CommandsGrpcService> logger)
    {
        _commandService = commandService;
        _logger         = logger;
    }

    public override async Task<UpdateCommandStatusReply> UpdateCommandStatus(
        UpdateCommandStatusRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.TrackingNumber) || request.KeyCarrier == 0)
        {
            _logger.LogWarning(
                "UpdateCommandStatus: invalid request (tracking={Num}, carrier={Carrier}, action={Action}).",
                request.TrackingNumber, request.KeyCarrier, request.Action);
            return new UpdateCommandStatusReply { Success = false };
        }

        try
        {
            await _commandService.UpdateCommandStatusByTracking(
                request.TrackingNumber, request.KeyCarrier, request.Action);

            _logger.LogInformation(
                "UpdateCommandStatus: action={Action} tracking={Num} carrier={Carrier} — done.",
                request.Action, request.TrackingNumber, request.KeyCarrier);

            return new UpdateCommandStatusReply { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "UpdateCommandStatus: error for action={Action} tracking={Num} carrier={Carrier}.",
                request.Action, request.TrackingNumber, request.KeyCarrier);
            return new UpdateCommandStatusReply { Success = false };
        }
    }
}
