using ElectrostoreAPI.Validators;
using System.ComponentModel.DataAnnotations;

namespace ElectrostoreAPI.Dto;

public record AiChatHistoryMessageDto
{
    [Required(ErrorMessage = "{0} is required.")]
    public required string role { get; init; } // "user" | "assistant"

    [Required(ErrorMessage = "{0} is required.")]
    [MaxLength(Constants.MaxChatMessageLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    public required string content { get; init; }
}

public record CreateAiChatMessageDto
{
    [MaxLength(Constants.MaxChatMessageLength, ErrorMessage = "{0} cannot exceed {1} characters.")]
    [OptionalNotEmpty(ErrorMessage = "{0} cannot be empty or whitespace.")]
    public string? content_ai_chat_message { get; init; }

    [FileSize(nameof(Constants.MaxAudioSizeMB), ErrorMessage = "{0} cannot exceed {1} MB in size.")]
    [FileType(nameof(Constants.AllowedAudioMimeTypes),
        ErrorMessage = "{0} has an invalid file type, allowed types are: [{1}], and extensions are: [{2}].")]
    public IFormFile? audio { get; init; }

    public List<AiChatHistoryMessageDto>? history { get; init; }
}

public record ReadAiChatMessageDto
{
    public required string role { get; init; } // always "assistant" for a response
    public required string content { get; init; }
}

public record ProposedActionDto
{
    public required string action_type { get; init; }
    public required object payload { get; init; }
}

public record SendAiChatMessageResponseDto
{
    public required ReadAiChatMessageDto message { get; init; }
    public required IEnumerable<ProposedActionDto> proposed_actions { get; init; }
}

