using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;
using ElectrostoreAPI.Services.LlmChatService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class LlmChatServiceTests : TestBase
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler = new();
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();

        private HttpRequestMessage? _lastRequest;
        private string? _lastRequestBody;

        public LlmChatServiceTests()
        {
            _mockHttpClientFactory.Setup(f => f.CreateClient(LlmChatService.HttpClientName))
                .Returns(() => new HttpClient(_mockHttpMessageHandler.Object) { BaseAddress = new Uri("http://llm.test/") });
        }

        private LlmChatService CreateService(IConfiguration configuration) =>
            new(_mockHttpClientFactory.Object, configuration, CreateLogger<LlmChatService>());

        private const string NativeEndpoint = "api/chat";

        private static IConfiguration BuildConfiguration(bool enabled = true, string? model = null, string? apiKey = null, string endpoint = "v1/chat/completions", Dictionary<string, string?>? extra = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["Llm:Enable"] = enabled.ToString(),
                ["Llm:Endpoint"] = endpoint
            };
            foreach (var (key, value) in extra ?? [])
            {
                values[key] = value;
            }
            if (model is not null)
            {
                values["Llm:Model"] = model;
            }
            if (apiKey is not null)
            {
                values["Llm:ApiKey"] = apiKey;
            }
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static List<LlmMessage> BuildMessages() => [new LlmMessage { role = "user", content = "hello" }];

        private void SetupResponse(HttpStatusCode statusCode, string body)
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
                {
                    _lastRequest = request;
                    _lastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();
                    return new HttpResponseMessage { StatusCode = statusCode, Content = new StringContent(body) };
                });
        }

        private static string SseBody(params string[] dataLines) => string.Join("\n\n", dataLines) + "\n\n";

        private static string Chunk(string content) =>
            "data: " + JsonSerializer.Serialize(new { choices = new[] { new { delta = new { content } } } });

        private static async Task<List<string>> Collect(IAsyncEnumerable<string> stream)
        {
            var result = new List<string>();
            await foreach (var item in stream)
            {
                result.Add(item);
            }
            return result;
        }

        // --- IsEnabled ---

        [Fact]
        public void IsEnabled_ShouldReflectConfiguration()
        {
            Assert.True(CreateService(BuildConfiguration(enabled: true)).IsEnabled);
            Assert.False(CreateService(BuildConfiguration(enabled: false)).IsEnabled);
        }

        [Fact]
        public void IsEnabled_ShouldBeFalse_WhenNotConfigured()
        {
            var service = CreateService(new ConfigurationBuilder().Build());

            Assert.False(service.IsEnabled);
        }

        // --- GetChatCompletionAsync ---

        [Fact]
        public async Task GetChatCompletionAsync_ShouldThrowInvalidOperationException_WhenDisabled()
        {
            var service = CreateService(BuildConfiguration(enabled: false));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetChatCompletionAsync(BuildMessages()));
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldReturnContentAndFinishReason()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"hi there"},"finish_reason":"stop"}]}""");
            var service = CreateService(BuildConfiguration());

            var result = await service.GetChatCompletionAsync(BuildMessages());

            Assert.Equal("hi there", result.content);
            Assert.Equal("stop", result.finish_reason);
            Assert.Null(result.tool_calls);
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldReturnToolCalls()
        {
            SetupResponse(HttpStatusCode.OK, """
                {"choices":[{"message":{"role":"assistant","content":null,"tool_calls":[{"id":"call-1","type":"function","function":{"name":"search_items","arguments":"{\"q\":\"r\"}"}}]},"finish_reason":"tool_calls"}]}
                """);
            var service = CreateService(BuildConfiguration());

            var result = await service.GetChatCompletionAsync(BuildMessages());

            var call = Assert.Single(result.tool_calls!);
            Assert.Equal("call-1", call.id);
            Assert.Equal("search_items", call.function.name);
            Assert.Equal("""{"q":"r"}""", call.function.arguments);
            Assert.Equal("tool_calls", result.finish_reason);
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldPostToConfiguredEndpointWithModelAndMessages()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""");
            var service = CreateService(BuildConfiguration(model: "my-model"));

            await service.GetChatCompletionAsync(BuildMessages());

            Assert.Equal(HttpMethod.Post, _lastRequest!.Method);
            Assert.Equal("http://llm.test/v1/chat/completions", _lastRequest.RequestUri!.ToString());
            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.Equal("my-model", body.RootElement.GetProperty("model").GetString());
            Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
            Assert.Equal("hello", body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString());
            Assert.False(body.RootElement.TryGetProperty("tools", out _));
            Assert.Null(_lastRequest.Headers.Authorization);
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldUseDefaultModel_WhenNoneConfigured()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""");
            var service = CreateService(BuildConfiguration());

            await service.GetChatCompletionAsync(BuildMessages());

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.Equal("llama3.1:8b", body.RootElement.GetProperty("model").GetString());
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldSendBearerToken_WhenApiKeyConfigured()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""");
            var service = CreateService(BuildConfiguration(apiKey: "secret"));

            await service.GetChatCompletionAsync(BuildMessages());

            Assert.Equal("Bearer", _lastRequest!.Headers.Authorization!.Scheme);
            Assert.Equal("secret", _lastRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldIncludeTools_WhenProvided()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""");
            var service = CreateService(BuildConfiguration());
            var tools = new List<LlmToolDefinition>
            {
                new() { function = new LlmFunctionDefinition { name = "search_items", description = "search", parameters = new { type = "object" } } }
            };

            await service.GetChatCompletionAsync(BuildMessages(), tools);

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.Equal("auto", body.RootElement.GetProperty("tool_choice").GetString());
            Assert.Equal("search_items", body.RootElement.GetProperty("tools")[0].GetProperty("function").GetProperty("name").GetString());
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldOmitTools_WhenListIsEmpty()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""");
            var service = CreateService(BuildConfiguration());

            await service.GetChatCompletionAsync(BuildMessages(), []);

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.False(body.RootElement.TryGetProperty("tools", out _));
            Assert.False(body.RootElement.TryGetProperty("tool_choice", out _));
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldThrowHttpRequestException_WhenStatusIsNotSuccess()
        {
            SetupResponse(HttpStatusCode.InternalServerError, "boom");
            var service = CreateService(BuildConfiguration());

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.GetChatCompletionAsync(BuildMessages()));

            Assert.Contains("500", ex.Message);
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldRethrowHttpRequestException_WhenTransportFails()
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("connection refused"));
            var service = CreateService(BuildConfiguration());

            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetChatCompletionAsync(BuildMessages()));
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldThrowLlmException_WhenBodyIsNotJson()
        {
            SetupResponse(HttpStatusCode.OK, "<html>not json</html>");
            var service = CreateService(BuildConfiguration());

            await Assert.ThrowsAsync<LlmException>(() => service.GetChatCompletionAsync(BuildMessages()));
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldThrowLlmException_WhenBodyIsJsonNull()
        {
            SetupResponse(HttpStatusCode.OK, "null");
            var service = CreateService(BuildConfiguration());

            await Assert.ThrowsAsync<LlmException>(() => service.GetChatCompletionAsync(BuildMessages()));
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldThrowLlmException_WhenNoChoicesAndNoMessage()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[]}""");
            var service = CreateService(BuildConfiguration());

            await Assert.ThrowsAsync<LlmException>(() => service.GetChatCompletionAsync(BuildMessages()));
        }

        // --- GetChatCompletionAsync: tolerance to the answer format ---

        private const string NativeToolCallBody = """
            {"model":"qwen3.5:9b","message":{"role":"assistant","content":"","thinking":"hmm","tool_calls":[
                {"id":"call_1","function":{"index":0,"name":"create_item","arguments":{"reference_name_item":"R-1","threshold_min_item":50}}},
                {"function":{"index":1,"name":"create_item","arguments":{"reference_name_item":"R-2"}}}]},
             "done":true,"done_reason":"stop"}
            """;

        [Fact]
        public async Task GetChatCompletionAsync_ShouldReadNativeAnswer_WhenMessageIsAtTheRoot()
        {
            SetupResponse(HttpStatusCode.OK, """{"message":{"role":"assistant","content":"hi"},"done":true,"done_reason":"stop"}""");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint));

            var result = await service.GetChatCompletionAsync(BuildMessages());

            Assert.Equal("hi", result.content);
            Assert.Equal("stop", result.finish_reason);
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldReadToolCallArgumentsAsJsonText_WhenTheyAreAnObject()
        {
            SetupResponse(HttpStatusCode.OK, NativeToolCallBody);
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint));

            var result = await service.GetChatCompletionAsync(BuildMessages());

            Assert.Equal(2, result.tool_calls!.Count);
            Assert.Equal("call_1", result.tool_calls[0].id);
            using var arguments = JsonDocument.Parse(result.tool_calls[0].function.arguments);
            Assert.Equal("R-1", arguments.RootElement.GetProperty("reference_name_item").GetString());
            Assert.Equal(50, arguments.RootElement.GetProperty("threshold_min_item").GetInt32());
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldGenerateAnId_WhenTheToolCallHasNone()
        {
            SetupResponse(HttpStatusCode.OK, NativeToolCallBody);
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint));

            var result = await service.GetChatCompletionAsync(BuildMessages());

            Assert.False(string.IsNullOrEmpty(result.tool_calls![1].id));
            Assert.NotEqual(result.tool_calls[0].id, result.tool_calls[1].id);
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldReadOpenAiAnswer_WhenEndpointIsNative()
        {
            // the format of the answer decides how it is read, not the configured endpoint
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"hi"},"finish_reason":"stop"}]}""");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint));

            var result = await service.GetChatCompletionAsync(BuildMessages());

            Assert.Equal("hi", result.content);
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldThrowLlmException_WhenRequestTimesOut()
        {
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("timeout", new TimeoutException()));
            var service = CreateService(BuildConfiguration());

            await Assert.ThrowsAsync<LlmException>(() => service.GetChatCompletionAsync(BuildMessages()));
        }

        // --- GetChatCompletionAsync: request dialects ---

        private static List<LlmMessage> BuildToolConversation() =>
        [
            new() { role = "user", content = "create R-1" },
            new() { role = "assistant", tool_calls = [new LlmToolCall { id = "call_1", function = new LlmToolCallFunction { name = "create_item", arguments = """{"reference_name_item":"R-1"}""" } }] },
            new() { role = "tool", content = "{}", tool_call_id = "call_1", name = "create_item" }
        ];

        [Fact]
        public async Task GetChatCompletionAsync_ShouldSendArgumentsAsString_ForOpenAiEndpoint()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""");
            var service = CreateService(BuildConfiguration());

            await service.GetChatCompletionAsync(BuildToolConversation());

            using var body = JsonDocument.Parse(_lastRequestBody!);
            var messages = body.RootElement.GetProperty("messages");
            Assert.Equal(JsonValueKind.String, messages[1].GetProperty("tool_calls")[0].GetProperty("function").GetProperty("arguments").ValueKind);
            Assert.Equal("call_1", messages[2].GetProperty("tool_call_id").GetString());
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldSendArgumentsAsObjectAndToolName_ForNativeEndpoint()
        {
            SetupResponse(HttpStatusCode.OK, """{"message":{"role":"assistant","content":"ok"}}""");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint));
            var tools = new List<LlmToolDefinition> { new() { function = new LlmFunctionDefinition { name = "create_item" } } };

            await service.GetChatCompletionAsync(BuildToolConversation(), tools);

            using var body = JsonDocument.Parse(_lastRequestBody!);
            var messages = body.RootElement.GetProperty("messages");
            var arguments = messages[1].GetProperty("tool_calls")[0].GetProperty("function").GetProperty("arguments");
            Assert.Equal(JsonValueKind.Object, arguments.ValueKind);
            Assert.Equal("R-1", arguments.GetProperty("reference_name_item").GetString());
            Assert.Equal("create_item", messages[2].GetProperty("tool_name").GetString());
            Assert.False(body.RootElement.TryGetProperty("tool_choice", out _));
            Assert.Equal("create_item", body.RootElement.GetProperty("tools")[0].GetProperty("function").GetProperty("name").GetString());
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldSendEmptyArguments_WhenTheyAreNotJson_ForNativeEndpoint()
        {
            SetupResponse(HttpStatusCode.OK, """{"message":{"role":"assistant","content":"ok"}}""");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint));
            var messages = new List<LlmMessage>
            {
                new() { role = "assistant", tool_calls = [new LlmToolCall { id = "c", function = new LlmToolCallFunction { name = "list_stores", arguments = "{broken" } }] }
            };

            await service.GetChatCompletionAsync(messages);

            using var body = JsonDocument.Parse(_lastRequestBody!);
            var arguments = body.RootElement.GetProperty("messages")[0].GetProperty("tool_calls")[0].GetProperty("function").GetProperty("arguments");
            Assert.Equal(JsonValueKind.Object, arguments.ValueKind);
        }

        // --- GetChatCompletionAsync: think and temperature ---

        [Fact]
        public async Task GetChatCompletionAsync_ShouldDisableThinking_ByDefaultOnNativeEndpoint()
        {
            SetupResponse(HttpStatusCode.OK, """{"message":{"role":"assistant","content":"ok"}}""");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint));

            await service.GetChatCompletionAsync(BuildMessages());

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.False(body.RootElement.GetProperty("think").GetBoolean());
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldNotSendThink_WhenNotConfiguredOnOpenAiEndpoint()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""");
            var service = CreateService(BuildConfiguration());

            await service.GetChatCompletionAsync(BuildMessages());

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.False(body.RootElement.TryGetProperty("think", out _));
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldEnableThinking_WhenConfigured()
        {
            SetupResponse(HttpStatusCode.OK, """{"message":{"role":"assistant","content":"ok"}}""");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint, extra: new() { ["Llm:Think"] = "true" }));

            await service.GetChatCompletionAsync(BuildMessages());

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.True(body.RootElement.GetProperty("think").GetBoolean());
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldLetTheCallOverrideThink()
        {
            SetupResponse(HttpStatusCode.OK, """{"message":{"role":"assistant","content":"ok"}}""");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint, extra: new() { ["Llm:Think"] = "false" }));

            await service.GetChatCompletionAsync(BuildMessages(), null, new LlmChatOptions { Think = true });

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.True(body.RootElement.GetProperty("think").GetBoolean());
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldSendTemperatureInOptions_ForNativeEndpoint()
        {
            SetupResponse(HttpStatusCode.OK, """{"message":{"role":"assistant","content":"ok"}}""");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint, extra: new() { ["Llm:Temperature"] = "0.2" }));

            await service.GetChatCompletionAsync(BuildMessages());

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.Equal(0.2, body.RootElement.GetProperty("options").GetProperty("temperature").GetDouble());
            Assert.False(body.RootElement.TryGetProperty("temperature", out _));
        }

        [Fact]
        public async Task GetChatCompletionAsync_ShouldSendTemperatureAtTheRoot_ForOpenAiEndpoint()
        {
            SetupResponse(HttpStatusCode.OK, """{"choices":[{"message":{"role":"assistant","content":"ok"}}]}""");
            var service = CreateService(BuildConfiguration(extra: new() { ["Llm:Temperature"] = "0.2" }));

            await service.GetChatCompletionAsync(BuildMessages(), null, new LlmChatOptions { Temperature = 0.7 });

            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.Equal(0.7, body.RootElement.GetProperty("temperature").GetDouble());
            Assert.False(body.RootElement.TryGetProperty("options", out _));
        }

        // --- StreamChatCompletionAsync ---

        [Fact]
        public async Task StreamChatCompletionAsync_ShouldReadNativeLinesUntilDone()
        {
            SetupResponse(HttpStatusCode.OK, string.Join("\n", [
                """{"message":{"role":"assistant","content":"Hel"},"done":false}""",
                """{"message":{"role":"assistant","content":"lo"},"done":false}""",
                """{"message":{"role":"assistant","content":""},"done":true}""",
                """{"message":{"role":"assistant","content":"ignored"},"done":false}"""]) + "\n");
            var service = CreateService(BuildConfiguration(endpoint: NativeEndpoint));

            var result = await Collect(service.StreamChatCompletionAsync(BuildMessages()));

            Assert.Equal(["Hel", "lo"], result);
            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.False(body.RootElement.GetProperty("think").GetBoolean());
        }

        [Fact]
        public async Task StreamChatCompletionAsync_ShouldThrowInvalidOperationException_WhenDisabled()
        {
            var service = CreateService(BuildConfiguration(enabled: false));

            await Assert.ThrowsAsync<InvalidOperationException>(() => Collect(service.StreamChatCompletionAsync(BuildMessages())));
        }

        [Fact]
        public async Task StreamChatCompletionAsync_ShouldYieldDeltasUntilDone()
        {
            SetupResponse(HttpStatusCode.OK, SseBody(Chunk("Hel"), Chunk("lo"), "data: [DONE]", Chunk("ignored")));
            var service = CreateService(BuildConfiguration());

            var result = await Collect(service.StreamChatCompletionAsync(BuildMessages()));

            Assert.Equal(["Hel", "lo"], result);
            using var body = JsonDocument.Parse(_lastRequestBody!);
            Assert.True(body.RootElement.GetProperty("stream").GetBoolean());
            Assert.False(body.RootElement.TryGetProperty("tools", out _));
        }

        [Fact]
        public async Task StreamChatCompletionAsync_ShouldSkipNonDataLinesInvalidChunksAndEmptyDeltas()
        {
            SetupResponse(HttpStatusCode.OK, SseBody(
                ": keep-alive",
                "event: message",
                "data: {not-json",
                """data: {"choices":[]}""",
                """data: {"choices":[{"delta":{"content":""}}]}""",
                """data: {"choices":[{"delta":{}}]}""",
                Chunk("kept")));
            var service = CreateService(BuildConfiguration());

            var result = await Collect(service.StreamChatCompletionAsync(BuildMessages()));

            Assert.Equal(["kept"], result);
        }

        [Fact]
        public async Task StreamChatCompletionAsync_ShouldYieldNothing_WhenStreamHasNoData()
        {
            SetupResponse(HttpStatusCode.OK, "");
            var service = CreateService(BuildConfiguration());

            var result = await Collect(service.StreamChatCompletionAsync(BuildMessages()));

            Assert.Empty(result);
        }

        [Fact]
        public async Task StreamChatCompletionAsync_ShouldThrowHttpRequestException_WhenStatusIsNotSuccess()
        {
            SetupResponse(HttpStatusCode.BadGateway, "bad gateway");
            var service = CreateService(BuildConfiguration());

            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => Collect(service.StreamChatCompletionAsync(BuildMessages())));

            Assert.Contains("502", ex.Message);
        }

        [Fact]
        public async Task StreamChatCompletionAsync_ShouldStop_WhenCancellationIsRequested()
        {
            SetupResponse(HttpStatusCode.OK, SseBody(Chunk("a"), Chunk("b")));
            var service = CreateService(BuildConfiguration());
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = new List<string>();
            try
            {
                await foreach (var item in service.StreamChatCompletionAsync(BuildMessages(), cts.Token))
                {
                    result.Add(item);
                }
            }
            catch (OperationCanceledException)
            {
                // cancelled before the response was read, which is also a valid outcome
            }

            Assert.Empty(result);
        }
    }
}
