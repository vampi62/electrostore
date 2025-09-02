namespace electrostore.Dto;

public record GetFileResult
{
    public required string FilePath { get; init; }
    public required string MimeType { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}