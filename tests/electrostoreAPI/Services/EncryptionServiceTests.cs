using Xunit;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Services.EncryptionService;

namespace ElectrostoreAPI.Tests.Services
{
    public class EncryptionServiceTests
    {
        private const string ValidHexKey = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        private static EncryptionService CreateService() => new();

        // --- Encrypt ---

        [Fact]
        public async Task Encrypt_ShouldReturnCiphertextDifferentFromPlainText()
        {
            var service = CreateService();

            var result = await service.Encrypt("hello world", ValidHexKey);

            Assert.NotEmpty(result.encrypted_data);
            Assert.Equal(12, result.iv.Length);
            Assert.Equal(16, result.tag.Length);
        }

        [Fact]
        public async Task Encrypt_ShouldProduceDifferentCiphertext_OnEachCall()
        {
            var service = CreateService();

            var result1 = await service.Encrypt("hello world", ValidHexKey);
            var result2 = await service.Encrypt("hello world", ValidHexKey);

            Assert.NotEqual(Convert.ToHexString(result1.iv), Convert.ToHexString(result2.iv));
            Assert.NotEqual(Convert.ToHexString(result1.encrypted_data), Convert.ToHexString(result2.encrypted_data));
        }

        [Fact]
        public async Task Encrypt_ShouldThrowArgumentException_WhenKeyIsNot32Bytes()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.Encrypt("hello world", "aabbcc"));
        }

        // --- Decrypt ---

        [Fact]
        public async Task Decrypt_ShouldReturnOriginalPlainText_AfterEncrypt()
        {
            var service = CreateService();
            var encrypted = await service.Encrypt("hello world", ValidHexKey);

            var decrypted = await service.Decrypt(encrypted, ValidHexKey);

            Assert.Equal("hello world", decrypted);
        }

        [Fact]
        public async Task Decrypt_ShouldThrowInvalidOperationException_WhenKeyIsWrong()
        {
            var service = CreateService();
            var encrypted = await service.Encrypt("hello world", ValidHexKey);
            var wrongKey = "fedcba9876543210fedcba9876543210fedcba9876543210fedcba9876543210";

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.Decrypt(encrypted, wrongKey));
        }

        [Fact]
        public async Task Decrypt_ShouldThrowInvalidOperationException_WhenDataHasBeenTampered()
        {
            var service = CreateService();
            var encrypted = await service.Encrypt("hello world", ValidHexKey);
            var tampered = encrypted with { encrypted_data = (byte[])encrypted.encrypted_data.Clone() };
            tampered.encrypted_data[0] ^= 0xFF;

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.Decrypt(tampered, ValidHexKey));
        }

        [Fact]
        public async Task Decrypt_ShouldThrowArgumentException_WhenKeyIsNot32Bytes()
        {
            var service = CreateService();
            var encrypted = await service.Encrypt("hello world", ValidHexKey);

            await Assert.ThrowsAsync<ArgumentException>(() => service.Decrypt(encrypted, "aabbcc"));
        }
    }
}
