using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ElectrostoreAPI.Services.LlmChatService;

public class LlmChatService : ILlmChatService
{
    public const string HttpClientName = "Llm";

    private const string RequestFailedMessage = "LLM chat completion request failed with status";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LlmChatService> _logger;

    public LlmChatService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LlmChatService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsEnabled => _configuration.GetValue<bool>("Llm:Enable");

    // Ollama's own endpoint (api/chat) and the OpenAI-compatible one (v1/chat/completions) do not speak the same dialect
    private bool IsNativeDialect => (_configuration.GetValue<string>("Llm:Endpoint") ?? "").TrimStart('/').StartsWith("api/", StringComparison.OrdinalIgnoreCase);

    public Task<LlmChatResult> GetChatCompletionAsync(List<LlmMessage> messages, List<LlmToolDefinition>? tools = null, CancellationToken cancellationToken = default)
    {
        return GetChatCompletionAsync(messages, tools, new LlmChatOptions(), cancellationToken);
    }

    public async Task<LlmChatResult> GetChatCompletionAsync(List<LlmMessage> messages, List<LlmToolDefinition>? tools, LlmChatOptions options, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("LLM chat integration is disabled");
        }
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = BuildRequest(messages, tools, stream: false, options);
        _logger.LogDebug("Calling LLM chat completion endpoint {Url}", new Uri(client.BaseAddress!, request.RequestUri!));
        string responseBody;
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("LLM chat completion request failed with status {Status}: {Body}", response.StatusCode, body);
                throw new HttpRequestException($"{RequestFailedMessage} {(int)response.StatusCode}");
            }
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.Message.StartsWith(RequestFailedMessage, StringComparison.Ordinal))
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException("Error while calling the LLM chat completion endpoint", ex, ex.StatusCode);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout elapsed, the caller did not cancel
            _logger.LogError(ex, "LLM chat completion request timed out");
            throw new LlmException("The LLM did not answer in time", ex);
        }
        _logger.LogDebug("LLM chat completion response body: {Body}", responseBody);
        LlmChatCompletionResponse? completion;
        try
        {
            completion = JsonSerializer.Deserialize<LlmChatCompletionResponse>(responseBody);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "LLM chat completion response could not be parsed as JSON. Raw body: {Body}", responseBody);
            throw new LlmException("LLM chat completion response could not be parsed", ex);
        }
        if (completion is null)
        {
            _logger.LogError("LLM chat completion response deserialized to null. Raw body: {Body}", responseBody);
            throw new LlmException("LLM chat completion response could not be parsed");
        }
        // OpenAI-compatible backends answer with choices[], native Ollama with the message at the root
        var choice = completion.choices.FirstOrDefault();
        var message = choice?.message ?? completion.message;
        if (message is null)
        {
            _logger.LogError("LLM chat completion response contained neither 'choices' nor 'message'. Raw body: {Body}", responseBody);
            throw new LlmException("LLM chat completion returned no message");
        }
        return new LlmChatResult
        {
            content = message.content,
            tool_calls = message.tool_calls,
            finish_reason = choice?.finish_reason ?? completion.done_reason
        };
    }

    public IAsyncEnumerable<string> StreamChatCompletionAsync(List<LlmMessage> messages, CancellationToken cancellationToken = default)
    {
        return StreamChatCompletionAsync(messages, new LlmChatOptions(), cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamChatCompletionAsync(List<LlmMessage> messages, LlmChatOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("LLM chat integration is disabled");
        }
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = BuildRequest(messages, tools: null, stream: true, options);
        _logger.LogDebug("Calling LLM chat completion stream endpoint {Url}", new Uri(client.BaseAddress!, request.RequestUri!));
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("LLM chat completion stream request failed with status {Status}: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"LLM chat completion stream request failed with status {(int)response.StatusCode}");
        }
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using (stream)
        {
            using var reader = new StreamReader(stream);
            while (!cancellationToken.IsCancellationRequested && await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                // OpenAI-compatible backends stream server-sent events ("data: {...}"), native Ollama one JSON object per line
                var isEvent = line.StartsWith("data:", StringComparison.Ordinal);
                if (!isEvent && !line.StartsWith('{'))
                {
                    continue;
                }
                var data = isEvent ? line["data:".Length..].Trim() : line;
                if (data == "[DONE]")
                {
                    yield break;
                }
                LlmChatCompletionChunk? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<LlmChatCompletionChunk>(data);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping unparsable LLM stream chunk: {Data}", data);
                    continue;
                }
                var delta = chunk?.choices.FirstOrDefault()?.delta?.content ?? chunk?.message?.content;
                if (!string.IsNullOrEmpty(delta))
                {
                    yield return delta;
                }
                if (chunk?.done == true)
                {
                    yield break;
                }
            }
        }
    }

    private HttpRequestMessage BuildRequest(List<LlmMessage> messages, List<LlmToolDefinition>? tools, bool stream, LlmChatOptions options)
    {
        var native = IsNativeDialect;
        var model = _configuration.GetValue<string>("Llm:Model") ?? "llama3.1:8b";
        var apiKey = _configuration.GetValue<string>("Llm:ApiKey");
        var temperature = options.Temperature ?? _configuration.GetValue<double?>("Llm:Temperature");
        // a reasoning model thinks for a long time before answering: off unless asked, native Ollama thinks by default
        var think = options.Think ?? _configuration.GetValue<bool?>("Llm:Think") ?? (native ? false : null);
        var request = new HttpRequestMessage(HttpMethod.Post, _configuration.GetValue<string>("Llm:Endpoint") ?? "");
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = native ? messages.Select(ToNativeMessage).ToList() : messages,
            ["stream"] = stream
        };
        if (tools is { Count: > 0 })
        {
            payload["tools"] = tools;
            if (!native)
            {
                payload["tool_choice"] = "auto";
            }
        }
        if (temperature is not null)
        {
            if (native)
            {
                payload["options"] = new { temperature };
            }
            else
            {
                payload["temperature"] = temperature;
            }
        }
        if (think is not null)
        {
            payload["think"] = think;
        }
        _logger.LogDebug("LLM chat completion request body: {Body}", JsonSerializer.Serialize(payload));
        request.Content = JsonContent.Create(payload);
        return request;
    }

    // Native Ollama wants the arguments of a tool call as an object (not a string) and names the tool a result belongs to
    private static Dictionary<string, object?> ToNativeMessage(LlmMessage message)
    {
        var native = new Dictionary<string, object?>
        {
            ["role"] = message.role,
            ["content"] = message.content ?? ""
        };
        if (message.tool_calls is { Count: > 0 })
        {
            native["tool_calls"] = message.tool_calls.Select(call => new
            {
                call.id,
                type = "function",
                function = new { call.function.name, arguments = ParseArguments(call.function.arguments) }
            }).ToList();
        }
        if (message.role == "tool" && message.name is not null)
        {
            native["tool_name"] = message.name;
        }
        return native;
    }

    private static JsonElement ParseArguments(string arguments)
    {
        try
        {
            using var document = JsonDocument.Parse(arguments);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var empty = JsonDocument.Parse("{}");
            return empty.RootElement.Clone();
        }
    }
}
