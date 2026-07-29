using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.EncryptionService;

public interface IEncryptionService
{
    Task<EncryptDto> Encrypt(string plainText, string hexKey);
    Task<string> Decrypt(EncryptDto encryptDto, string hexKey);
}