namespace ElectrostoreAPI.Services.LlmChatService;

public interface ILlmChatService
{
    bool IsEnabled { get; }

    Task<LlmChatResult> GetChatCompletionAsync(List<LlmMessage> messages, List<LlmToolDefinition>? tools = null, CancellationToken cancellationToken = default);

    /// <summary>Same, with settings for this call only (e.g. let the model think).</summary>
    Task<LlmChatResult> GetChatCompletionAsync(List<LlmMessage> messages, List<LlmToolDefinition>? tools, LlmChatOptions options, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamChatCompletionAsync(List<LlmMessage> messages, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamChatCompletionAsync(List<LlmMessage> messages, LlmChatOptions options, CancellationToken cancellationToken = default);
}
