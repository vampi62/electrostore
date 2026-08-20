using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ElectrostoreAPI.Services.LlmChatService;

public class LlmChatService : ILlmChatService
{
    public const string HttpClientName = "Llm";

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

    public async Task<LlmChatResult> GetChatCompletionAsync(List<LlmMessage> messages, List<LlmToolDefinition>? tools = null, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("LLM chat integration is disabled");
        }
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = BuildRequest(messages, tools, stream: false);
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error while calling the LLM chat completion endpoint");
            throw;
        }
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("LLM chat completion request failed with status {Status}: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"LLM chat completion request failed with status {(int)response.StatusCode}");
        }
        var completion = await response.Content.ReadFromJsonAsync<LlmChatCompletionResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("LLM chat completion response could not be parsed");
        var choice = completion.choices.FirstOrDefault() ?? throw new InvalidOperationException("LLM chat completion returned no choices");
        return new LlmChatResult
        {
            content = choice.message?.content,
            tool_calls = choice.message?.tool_calls,
            finish_reason = choice.finish_reason
        };
    }

    public async IAsyncEnumerable<string> StreamChatCompletionAsync(List<LlmMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("LLM chat integration is disabled");
        }
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = BuildRequest(messages, tools: null, stream: true);
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
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                {
                    continue;
                }
                var data = line["data:".Length..].Trim();
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
                var delta = chunk?.choices.FirstOrDefault()?.delta?.content;
                if (!string.IsNullOrEmpty(delta))
                {
                    yield return delta;
                }
            }
        }
    }

    private HttpRequestMessage BuildRequest(List<LlmMessage> messages, List<LlmToolDefinition>? tools, bool stream)
    {
        var model = _configuration.GetValue<string>("Llm:ChatModel") ?? "llama3.1";
        var apiKey = _configuration.GetValue<string>("Llm:ApiKey");
        var request = new HttpRequestMessage(HttpMethod.Post, _configuration.GetValue<string>("Llm:ChatEndpoint") ?? "");
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        if (tools is { Count: > 0 })
        {
            request.Content = JsonContent.Create(new
            {
                model,
                messages,
                stream,
                tools,
                tool_choice = "auto"
            });
        }
        else
        {
            request.Content = JsonContent.Create(new
            {
                model,
                messages,
                stream
            });
        }
        return request;
    }
}
