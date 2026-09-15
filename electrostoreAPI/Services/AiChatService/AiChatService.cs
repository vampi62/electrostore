using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.AiToolExecutorService;
using ElectrostoreAPI.Services.LlmChatService;
using ElectrostoreAPI.Services.SttService;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace ElectrostoreAPI.Services.AiChatService;

public class AiChatService : IAiChatService
{
    // Cap the tool-calling loop at 5 iterations (InvalidOperationException if exceeded).
    private const int MaxToolIterations = 5;

    private const string DefaultSystemPrompt =
        "You are the inventory management assistant for electrostore. You can look up items, boxes, " +
        "stores and tags using the tools made available to you. Any action that modifies the inventory " +
        "(creating an item, creating a tag, attaching a tag to an item, moving/adjusting stock in a box, " +
        "attaching a datasheet to an item) MUST be proposed through the corresponding tool. Proposing an " +
        "action does not apply it - never tell the user an action has been completed, only that it has " +
        "been proposed for their review.";

    private readonly ILlmChatService _llmChatService;
    private readonly ISttService _sttService;
    private readonly IAiToolExecutorService _aiToolExecutorService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiChatService> _logger;

    public AiChatService(
        ILlmChatService llmChatService,
        ISttService sttService,
        IAiToolExecutorService aiToolExecutorService,
        IConfiguration configuration,
        ILogger<AiChatService> logger)
    {
        _llmChatService = llmChatService;
        _sttService = sttService;
        _aiToolExecutorService = aiToolExecutorService;
        _configuration = configuration;
        _logger = logger;
    }

    private string SystemPrompt => _configuration.GetValue<string>("Llm:SystemPrompt") ?? DefaultSystemPrompt;

    public async Task<SendAiChatMessageResponseDto> SendMessage(CreateAiChatMessageDto messageDto, CancellationToken cancellationToken = default)
    {
        if (!_llmChatService.IsEnabled)
        {
            _logger.LogWarning("Rejected SendMessage request because the LLM chat integration is disabled");
            throw new InvalidOperationException("LLM chat integration is disabled");
        }
        var userText = await ResolveUserText(messageDto, cancellationToken);
        var messages = BuildConversation(messageDto, userText);
        var toolDefs = _aiToolExecutorService.GetToolDefinitions();
        var proposedActions = new List<ProposedActionDto>();
        LlmChatResult? result = null;
        for (var i = 0; i < MaxToolIterations; i++)
        {
            result = await _llmChatService.GetChatCompletionAsync(messages, toolDefs, cancellationToken);
            if (result.tool_calls is not { Count: > 0 })
            {
                _logger.LogDebug("SendMessage resolved after {Iteration} iteration(s) with no further tool calls", i + 1);
                break;
            }
            _logger.LogDebug("SendMessage iteration {Iteration}/{MaxIterations}: LLM requested {ToolCallCount} tool call(s)",
                i + 1, MaxToolIterations, result.tool_calls.Count);
            proposedActions.AddRange(await AppendToolResultsAsync(messages, result, cancellationToken));
            result = null;
        }
        if (result is null)
        {
            _logger.LogError("SendMessage tool-calling loop exceeded the maximum of {MaxIterations} iterations", MaxToolIterations);
            throw new InvalidOperationException("The assistant tool-calling loop exceeded the maximum number of iterations");
        }

        _logger.LogInformation("SendMessage completed with {ProposedActionCount} proposed action(s)", proposedActions.Count);
        return new SendAiChatMessageResponseDto
        {
            message = new ReadAiChatMessageDto { role = "assistant", content = result.content ?? string.Empty },
            proposed_actions = proposedActions
        };
    }

    public async Task StreamMessage(CreateAiChatMessageDto messageDto, HttpResponse httpResponse, CancellationToken cancellationToken = default)
    {
        if (!_llmChatService.IsEnabled)
        {
            _logger.LogWarning("Rejected StreamMessage request because the LLM chat integration is disabled");
            throw new InvalidOperationException("LLM chat integration is disabled");
        }
        var userText = await ResolveUserText(messageDto, cancellationToken);
        var messages = BuildConversation(messageDto, userText);
        var toolDefs = _aiToolExecutorService.GetToolDefinitions();
        var proposedActions = new List<ProposedActionDto>();
        var resolved = false;
        for (var i = 0; i < MaxToolIterations; i++)
        {
            var result = await _llmChatService.GetChatCompletionAsync(messages, toolDefs, cancellationToken);
            if (result.tool_calls is not { Count: > 0 })
            {
                _logger.LogDebug("StreamMessage resolved after {Iteration} iteration(s) with no further tool calls", i + 1);
                resolved = true;
                break;
            }
            _logger.LogDebug("StreamMessage iteration {Iteration}/{MaxIterations}: LLM requested {ToolCallCount} tool call(s)",
                i + 1, MaxToolIterations, result.tool_calls.Count);
            proposedActions.AddRange(await AppendToolResultsAsync(messages, result, cancellationToken));
        }
        if (!resolved)
        {
            _logger.LogError("StreamMessage tool-calling loop exceeded the maximum of {MaxIterations} iterations", MaxToolIterations);
            throw new InvalidOperationException("The assistant tool-calling loop exceeded the maximum number of iterations");
        }

        httpResponse.ContentType = "text/event-stream";
        httpResponse.Headers.CacheControl = "no-cache";
        httpResponse.Headers["X-Accel-Buffering"] = "no";

        _logger.LogDebug("StreamMessage starting delta stream with {ProposedActionCount} proposed action(s)", proposedActions.Count);
        var contentBuilder = new StringBuilder();
        await foreach (var delta in _llmChatService.StreamChatCompletionAsync(messages, cancellationToken))
        {
            contentBuilder.Append(delta);
            await httpResponse.WriteAsync($"data: {JsonSerializer.Serialize(new { delta })}\n\n", cancellationToken);
            await httpResponse.Body.FlushAsync(cancellationToken);
        }

        var donePayload = JsonSerializer.Serialize(new
        {
            message = new ReadAiChatMessageDto { role = "assistant", content = contentBuilder.ToString() },
            proposed_actions = proposedActions
        });
        await httpResponse.WriteAsync($"event: done\ndata: {donePayload}\n\n", cancellationToken);
        await httpResponse.Body.FlushAsync(cancellationToken);
        _logger.LogInformation("StreamMessage completed, streamed {Length} character(s) with {ProposedActionCount} proposed action(s)",
            contentBuilder.Length, proposedActions.Count);
    }

    // ---- shared helpers ----

    private async Task<List<ProposedActionDto>> AppendToolResultsAsync(List<LlmMessage> messages, LlmChatResult result, CancellationToken cancellationToken)
    {
        var proposedActions = new List<ProposedActionDto>();
        messages.Add(new LlmMessage { role = "assistant", content = result.content, tool_calls = result.tool_calls });
        foreach (var toolCall in result.tool_calls!)
        {
            _logger.LogDebug("Executing tool {ToolName} (call id {ToolCallId})", toolCall.function.name, toolCall.id);
            var execResult = await _aiToolExecutorService.ExecuteToolAsync(toolCall.function.name, toolCall.function.arguments, cancellationToken);
            if (execResult.ProposedAction is not null)
            {
                _logger.LogDebug("Tool {ToolName} produced a proposed action of type {ActionType}", toolCall.function.name, execResult.ProposedAction.action_type);
                proposedActions.Add(execResult.ProposedAction);
            }
            messages.Add(new LlmMessage
            {
                role = "tool",
                content = execResult.ResultJson,
                tool_call_id = toolCall.id,
                name = toolCall.function.name
            });
        }
        return proposedActions;
    }

    private async Task<string> ResolveUserText(CreateAiChatMessageDto messageDto, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(messageDto.content_ai_chat_message))
        {
            return messageDto.content_ai_chat_message;
        }
        if (messageDto.audio is not null)
        {
            if (!_sttService.IsEnabled)
            {
                _logger.LogWarning("Rejected chat message with audio because the STT integration is disabled");
                throw new InvalidOperationException("STT integration is disabled");
            }
            _logger.LogDebug("Transcribing audio message via STT");
            var transcribed = await _sttService.TranscribeAsync(messageDto.audio, cancellationToken);
            if (string.IsNullOrWhiteSpace(transcribed))
            {
                _logger.LogWarning("Audio transcription returned no text");
                throw new InvalidOperationException("Audio transcription returned no text");
            }
            _logger.LogDebug("Audio transcription succeeded, {Length} character(s)", transcribed.Length);
            return transcribed;
        }
        _logger.LogWarning("Rejected chat message with neither text content nor audio");
        throw new ArgumentException("Either content_ai_chat_message or audio must be provided");
    }

    private List<LlmMessage> BuildConversation(CreateAiChatMessageDto messageDto, string userText)
    {
        var messages = new List<LlmMessage>
        {
            new() { role = "system", content = SystemPrompt }
        };
        if (messageDto.history is { Count: > 0 })
        {
            messages.AddRange(messageDto.history.Select(h => new LlmMessage { role = h.role, content = h.content }));
        }
        messages.Add(new LlmMessage { role = "user", content = userText });
        return messages;
    }
}
