using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.AiChatService;
using ElectrostoreAPI.Services.AiToolExecutorService;
using ElectrostoreAPI.Services.LlmChatService;
using ElectrostoreAPI.Services.SttService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Services
{
    public class AiChatServiceTests : TestBase
    {
        private readonly Mock<ILlmChatService> _llmChatService = new();
        private readonly Mock<ISttService> _sttService = new();
        private readonly Mock<IAiToolExecutorService> _aiToolExecutorService = new();

        public AiChatServiceTests()
        {
            _llmChatService.Setup(l => l.IsEnabled).Returns(true);
            _aiToolExecutorService.Setup(t => t.GetToolDefinitions()).Returns(new List<LlmToolDefinition>());
        }

        private AiChatService CreateService(IConfiguration? configuration = null) =>
            new(_llmChatService.Object, _sttService.Object, _aiToolExecutorService.Object,
                configuration ?? new ConfigurationBuilder().Build(), CreateLogger<AiChatService>());

        private static async IAsyncEnumerable<string> ToAsyncEnumerable(params string[] items)
        {
            foreach (var item in items)
            {
                yield return item;
                await Task.Yield();
            }
        }

        // --- SendMessage ---

        [Fact]
        public async Task SendMessage_ShouldThrowInvalidOperationException_WhenLlmDisabled()
        {
            _llmChatService.Setup(l => l.IsEnabled).Returns(false);
            var service = CreateService();
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "hello" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessage(dto));
        }

        [Fact]
        public async Task SendMessage_ShouldThrowArgumentException_WhenNoContentAndNoAudio()
        {
            var service = CreateService();
            var dto = new CreateAiChatMessageDto();

            await Assert.ThrowsAsync<ArgumentException>(() => service.SendMessage(dto));
        }

        [Fact]
        public async Task SendMessage_ShouldReturnAssistantMessage_WhenNoToolCalls()
        {
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LlmChatResult { content = "Hi there" });
            var service = CreateService();
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "hello" };

            var result = await service.SendMessage(dto);

            Assert.Equal("assistant", result.message.role);
            Assert.Equal("Hi there", result.message.content);
            Assert.Empty(result.proposed_actions);
        }

        [Fact]
        public async Task SendMessage_ShouldTranscribeAudio_WhenContentIsEmpty()
        {
            _sttService.Setup(s => s.IsEnabled).Returns(true);
            _sttService.Setup(s => s.TranscribeAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>())).ReturnsAsync("transcribed text");
            List<LlmMessage>? capturedMessages = null;
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .Callback<List<LlmMessage>, List<LlmToolDefinition>?, CancellationToken>((messages, _, _) => capturedMessages = messages)
                .ReturnsAsync(new LlmChatResult { content = "ok" });
            var service = CreateService();
            var audio = new Mock<IFormFile>();
            var dto = new CreateAiChatMessageDto { audio = audio.Object };

            await service.SendMessage(dto);

            Assert.NotNull(capturedMessages);
            Assert.Contains(capturedMessages!, m => m.role == "user" && m.content == "transcribed text");
        }

        [Fact]
        public async Task SendMessage_ShouldThrowInvalidOperationException_WhenSttDisabledAndAudioProvided()
        {
            _sttService.Setup(s => s.IsEnabled).Returns(false);
            var service = CreateService();
            var dto = new CreateAiChatMessageDto { audio = new Mock<IFormFile>().Object };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessage(dto));
        }

        [Fact]
        public async Task SendMessage_ShouldThrowInvalidOperationException_WhenTranscriptionReturnsEmpty()
        {
            _sttService.Setup(s => s.IsEnabled).Returns(true);
            _sttService.Setup(s => s.TranscribeAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>())).ReturnsAsync("   ");
            var service = CreateService();
            var dto = new CreateAiChatMessageDto { audio = new Mock<IFormFile>().Object };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessage(dto));
        }

        [Fact]
        public async Task SendMessage_ShouldExecuteToolsAndAccumulateProposedActions()
        {
            var toolCall = new LlmToolCall { id = "call-1", function = new LlmToolCallFunction { name = "create_tag" } };
            var callCount = 0;
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return callCount == 1
                        ? new LlmChatResult { tool_calls = new List<LlmToolCall> { toolCall } }
                        : new LlmChatResult { content = "Done" };
                });
            var proposedAction = new ProposedActionDto { action_type = "create_tag", payload = new { name = "urgent" } };
            _aiToolExecutorService.Setup(t => t.ExecuteToolAsync("create_tag", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiToolExecutionResult { ResultJson = "{}", ProposedAction = proposedAction });
            var service = CreateService();
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "create a tag" };

            var result = await service.SendMessage(dto);

            Assert.Equal("Done", result.message.content);
            Assert.Single(result.proposed_actions);
            Assert.Equal("create_tag", result.proposed_actions.First().action_type);
        }

        [Fact]
        public async Task SendMessage_ShouldThrowInvalidOperationException_WhenToolLoopExceedsMaxIterations()
        {
            var toolCall = new LlmToolCall { id = "call-1", function = new LlmToolCallFunction { name = "noop" } };
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LlmChatResult { tool_calls = new List<LlmToolCall> { toolCall } });
            _aiToolExecutorService.Setup(t => t.ExecuteToolAsync("noop", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiToolExecutionResult { ResultJson = "{}" });
            var service = CreateService();
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "loop forever" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendMessage(dto));
        }

        // --- StreamMessage ---

        [Fact]
        public async Task StreamMessage_ShouldThrowInvalidOperationException_WhenLlmDisabled()
        {
            _llmChatService.Setup(l => l.IsEnabled).Returns(false);
            var service = CreateService();
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "hello" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.StreamMessage(dto, context.Response));
        }

        [Fact]
        public async Task StreamMessage_ShouldWriteDeltasAndDoneEvent_WhenResolved()
        {
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LlmChatResult { content = "Hi there" });
            _llmChatService.Setup(l => l.StreamChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<CancellationToken>()))
                .Returns(ToAsyncEnumerable("Hi", " there"));
            var service = CreateService();
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "hello" };

            await service.StreamMessage(dto, context.Response);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            Assert.Equal("text/event-stream", context.Response.ContentType);
            Assert.Contains("\"delta\":\"Hi\"", body);
            Assert.Contains("event: done", body);
            Assert.Contains("Hi there", body);
        }

        [Fact]
        public async Task StreamMessage_ShouldThrowInvalidOperationException_WhenToolLoopExceedsMaxIterations()
        {
            var toolCall = new LlmToolCall { id = "call-1", function = new LlmToolCallFunction { name = "noop" } };
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LlmChatResult { tool_calls = new List<LlmToolCall> { toolCall } });
            _aiToolExecutorService.Setup(t => t.ExecuteToolAsync("noop", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiToolExecutionResult { ResultJson = "{}" });
            var service = CreateService();
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "loop forever" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.StreamMessage(dto, context.Response));
        }
    }
}
