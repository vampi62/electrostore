namespace ElectrostoreAPI.Dto;

public record EncryptDto
{
    public required byte[] EncryptedData { get; init; }
    public required byte[] IV { get; init; }
    public required byte[] Tag { get; init; }
}