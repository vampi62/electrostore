namespace electrostore.Dto;

public record GetFileResult
{
    public MemoryStream? FileStream { get; init; }
    public required string MimeType { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
public record SaveFileResult
{
    public required string url { get; init; }
    public required string mimeType { get; init; }
}