namespace ElectrostoreAPI.Dto;

public record EncryptDto
{
    public required byte[] encrypted_data { get; init; }
    public required byte[] iv { get; init; }
    public required byte[] tag { get; init; }
}