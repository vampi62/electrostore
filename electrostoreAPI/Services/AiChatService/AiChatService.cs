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
                break;
            }
            proposedActions.AddRange(await AppendToolResultsAsync(messages, result, cancellationToken));
            result = null;
        }
        if (result is null)
        {
            throw new InvalidOperationException("The assistant tool-calling loop exceeded the maximum number of iterations");
        }

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
                resolved = true;
                break;
            }
            proposedActions.AddRange(await AppendToolResultsAsync(messages, result, cancellationToken));
        }
        if (!resolved)
        {
            throw new InvalidOperationException("The assistant tool-calling loop exceeded the maximum number of iterations");
        }

        httpResponse.ContentType = "text/event-stream";
        httpResponse.Headers.CacheControl = "no-cache";
        httpResponse.Headers["X-Accel-Buffering"] = "no";

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
    }

    // ---- shared helpers ----

    private async Task<List<ProposedActionDto>> AppendToolResultsAsync(List<LlmMessage> messages, LlmChatResult result, CancellationToken cancellationToken)
    {
        var proposedActions = new List<ProposedActionDto>();
        messages.Add(new LlmMessage { role = "assistant", content = result.content, tool_calls = result.tool_calls });
        foreach (var toolCall in result.tool_calls!)
        {
            var execResult = await _aiToolExecutorService.ExecuteToolAsync(toolCall.function.name, toolCall.function.arguments, cancellationToken);
            if (execResult.ProposedAction is not null)
            {
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
                throw new InvalidOperationException("STT integration is disabled");
            }
            var transcribed = await _sttService.TranscribeAsync(messageDto.audio, cancellationToken);
            if (string.IsNullOrWhiteSpace(transcribed))
            {
                throw new InvalidOperationException("Audio transcription returned no text");
            }
            return transcribed;
        }
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
