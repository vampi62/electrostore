namespace ElectrostoreAPI.Dto;

public record GetFileResult
{
    public Stream? file_stream { get; init; }
    public required string mime_type { get; init; }
    public bool success { get; init; }
    public string? error_message { get; init; }
}
public record SaveFileResult
{
    public required string path { get; init; }
    public required string mime_type { get; init; }
}