using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.LlmChatService;

namespace ElectrostoreAPI.Services.AiToolExecutorService;

public record AiToolExecutionResult
{
    public required string ResultJson { get; init; }

    public ProposedActionDto? ProposedAction { get; init; }
}

public interface IAiToolExecutorService
{
    List<LlmToolDefinition> GetToolDefinitions();

    Task<AiToolExecutionResult> ExecuteToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}
