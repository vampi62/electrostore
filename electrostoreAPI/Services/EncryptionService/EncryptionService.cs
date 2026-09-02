using ElectrostoreAPI.Dto;
using System.Security.Cryptography;
using System.Text;

namespace ElectrostoreAPI.Services.EncryptionService;

public class EncryptionService : IEncryptionService
{
    public async Task<EncryptDto> Encrypt(string plainText, string hexKey)
    {
        byte[] key = Convert.FromHexString(hexKey);
        if (key.Length != 32)
        {
            throw new ArgumentException("Key must be 32 bytes (256 bits) long.");
        }
        using AesGcm aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);

        byte[] iv = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(iv);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] ciphertextBytes = new byte[plaintextBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];

        aesGcm.Encrypt(iv, plaintextBytes, ciphertextBytes, tag);

        return new EncryptDto
        {
            encrypted_data = ciphertextBytes,
            iv = iv,
            tag = tag
        };
    }

    public async Task<string> Decrypt(EncryptDto encryptDto, string hexKey)
    {
        byte[] key = Convert.FromHexString(hexKey);
        if (key.Length != 32)
        {
            throw new ArgumentException("Key must be 32 bytes (256 bits) long.");
        }
        using AesGcm aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        byte[] decryptedBytes = new byte[encryptDto.encrypted_data.Length];

        try
        {
            aesGcm.Decrypt(encryptDto.iv, encryptDto.encrypted_data, encryptDto.tag, decryptedBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException("Decryption failed. The data may have been tampered with or the key is incorrect.");
        }
    }
}