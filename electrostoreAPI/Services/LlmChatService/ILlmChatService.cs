namespace ElectrostoreAPI.Services.LlmChatService;

public interface ILlmChatService
{
    bool IsEnabled { get; }

    Task<LlmChatResult> GetChatCompletionAsync(List<LlmMessage> messages, List<LlmToolDefinition>? tools = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamChatCompletionAsync(List<LlmMessage> messages, CancellationToken cancellationToken = default);
}
