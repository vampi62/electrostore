using System.ComponentModel.DataAnnotations;

namespace electrostore.Dto;

public record GetFileResult
{
    public string FilePath { get; init; }
    public string MimeType { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}