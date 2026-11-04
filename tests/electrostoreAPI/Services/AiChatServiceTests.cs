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

        // --- SendMessage: attached files ---

        private static IFormFile BuildFile(string fileName, string contentType, byte[] content)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.Length).Returns(content.Length);
            file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
            return file.Object;
        }

        private List<LlmMessage> CaptureConversation()
        {
            var captured = new List<LlmMessage>();
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .Callback<List<LlmMessage>, List<LlmToolDefinition>?, CancellationToken>((messages, _, _) => captured.AddRange(messages))
                .ReturnsAsync(new LlmChatResult { content = "ok" });
            return captured;
        }

        [Fact]
        public async Task SendMessage_ShouldWriteTextFileInUserMessage()
        {
            var captured = CaptureConversation();
            var file = BuildFile("bom.csv", "application/vnd.ms-excel", Encoding.UTF8.GetBytes("ref;qty\nR1;10"));
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "create these items", files = [file] };

            await CreateService().SendMessage(dto);

            var user = captured.Single(m => m.role == "user");
            Assert.StartsWith("create these items", user.content);
            Assert.Contains("<attached_file name=\"bom.csv\">", user.content);
            Assert.Contains("ref;qty\nR1;10", user.content);
            Assert.Null(user.images);
        }

        [Fact]
        public async Task SendMessage_ShouldAttachImageAsBase64()
        {
            var captured = CaptureConversation();
            var file = BuildFile("board.png", "image/png", [1, 2, 3]);
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "what is it?", files = [file] };

            await CreateService().SendMessage(dto);

            var image = Assert.Single(captured.Single(m => m.role == "user").images!);
            Assert.Equal("image/png", image.mime_type);
            Assert.Equal(Convert.ToBase64String([1, 2, 3]), image.base64);
        }

        [Fact]
        public async Task SendMessage_ShouldAskToReadTheFiles_WhenThereIsOnlyFiles()
        {
            var captured = CaptureConversation();
            var dto = new CreateAiChatMessageDto { files = [BuildFile("note.txt", "text/plain", Encoding.UTF8.GetBytes("hello"))] };

            await CreateService().SendMessage(dto);

            Assert.StartsWith("Look at the attached file(s)", captured.Single(m => m.role == "user").content);
        }

        [Fact]
        public async Task SendMessage_ShouldKeepTheTextOfAudio_WhenFilesAreAttached()
        {
            _sttService.Setup(s => s.IsEnabled).Returns(true);
            _sttService.Setup(s => s.TranscribeAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>())).ReturnsAsync("transcribed text");
            var captured = CaptureConversation();
            var dto = new CreateAiChatMessageDto
            {
                audio = new Mock<IFormFile>().Object,
                files = [BuildFile("note.txt", "text/plain", Encoding.UTF8.GetBytes("hello"))]
            };

            await CreateService().SendMessage(dto);

            Assert.StartsWith("transcribed text", captured.Single(m => m.role == "user").content);
        }

        [Fact]
        public async Task SendMessage_ShouldTruncateAHugeTextFile()
        {
            var captured = CaptureConversation();
            var file = BuildFile("big.txt", "text/plain", Encoding.UTF8.GetBytes(new string('a', 60_000)));
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "read", files = [file] };

            await CreateService().SendMessage(dto);

            var content = captured.Single(m => m.role == "user").content!;
            Assert.Contains("[truncated]", content);
            Assert.True(content.Length < 55_000);
        }

        [Fact]
        public async Task SendMessage_ShouldThrowArgumentException_WhenTextFileIsBinary()
        {
            CaptureConversation();
            var file = BuildFile("fake.txt", "text/plain", [0x50, 0x4B, 0x00, 0x04]);
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "read", files = [file] };

            await Assert.ThrowsAsync<ArgumentException>(() => CreateService().SendMessage(dto));
        }

        [Fact]
        public async Task SendMessage_ShouldNotLetTheFileNameBreakThePrompt()
        {
            var captured = CaptureConversation();
            var file = BuildFile("x\"><evil>.txt", "text/plain", Encoding.UTF8.GetBytes("hello"));
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "read", files = [file] };

            await CreateService().SendMessage(dto);

            var content = captured.Single(m => m.role == "user").content!;
            Assert.Contains("<attached_file name=\"xevil.txt\">", content);
            Assert.DoesNotContain("<evil>", content);
        }

        [Fact]
        public async Task SendMessage_ShouldThrowArgumentException_WhenFileTypeIsUnsupported()
        {
            CaptureConversation();
            var file = BuildFile("program.exe", "application/octet-stream", [1, 2, 3]);
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "read", files = [file] };

            await Assert.ThrowsAsync<ArgumentException>(() => CreateService().SendMessage(dto));
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
        public async Task SendMessage_ShouldThrowLlmException_WhenToolLoopExceedsMaxIterations()
        {
            var toolCall = new LlmToolCall { id = "call-1", function = new LlmToolCallFunction { name = "noop" } };
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LlmChatResult { tool_calls = new List<LlmToolCall> { toolCall } });
            _aiToolExecutorService.Setup(t => t.ExecuteToolAsync("noop", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiToolExecutionResult { ResultJson = "{}" });
            var service = CreateService();
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "loop forever" };

            await Assert.ThrowsAsync<LlmException>(() => service.SendMessage(dto));
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
        public async Task StreamMessage_ShouldThrowLlmException_WhenToolLoopExceedsMaxIterations()
        {
            var toolCall = new LlmToolCall { id = "call-1", function = new LlmToolCallFunction { name = "noop" } };
            _llmChatService.Setup(l => l.GetChatCompletionAsync(It.IsAny<List<LlmMessage>>(), It.IsAny<List<LlmToolDefinition>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LlmChatResult { tool_calls = new List<LlmToolCall> { toolCall } });
            _aiToolExecutorService.Setup(t => t.ExecuteToolAsync("noop", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AiToolExecutionResult { ResultJson = "{}" });
            var service = CreateService();
            var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
            var dto = new CreateAiChatMessageDto { content_ai_chat_message = "loop forever" };

            await Assert.ThrowsAsync<LlmException>(() => service.StreamMessage(dto, context.Response));
        }
    }
}
