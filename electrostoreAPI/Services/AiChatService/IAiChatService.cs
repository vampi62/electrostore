using ElectrostoreAPI.Dto;
using Microsoft.AspNetCore.Http;

namespace ElectrostoreAPI.Services.AiChatService;

public interface IAiChatService
{
    Task<SendAiChatMessageResponseDto> SendMessage(CreateAiChatMessageDto messageDto, CancellationToken cancellationToken = default);

    Task StreamMessage(CreateAiChatMessageDto messageDto, HttpResponse httpResponse, CancellationToken cancellationToken = default);
}
