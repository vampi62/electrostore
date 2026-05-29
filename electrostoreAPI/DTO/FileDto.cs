namespace ElectrostoreAPI.Dto;

public record GetFileResult
{
    public Stream? FileStream { get; init; }
    public required string MimeType { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
public record SaveFileResult
{
    public required string path { get; init; }
    public required string mimeType { get; init; }
}