using System.Security.Cryptography;

namespace Networking
{
    public interface IEncryptor
    {
        public string Key { get; }
        public string IV { get; }
        public Task<byte[]> Encrypt(string data);
        public Task<string> Decrypt(byte[] data);
    }
    public sealed class Encryptor : IEncryptor
    {
        private readonly Aes aes = Aes.Create();
        public string Key => Convert.ToBase64String(aes.Key);
        public string IV => Convert.ToBase64String(aes.IV);

        public Encryptor()
        {
            this.aes.KeySize = 256;
            this.aes.BlockSize = 128;
            this.aes.Mode = CipherMode.CBC;
            this.aes.Padding = PaddingMode.PKCS7;
            this.aes.GenerateKey();
            this.aes.GenerateIV();
        }

        public async Task<byte[]> Encrypt(string data)
        {
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, this.aes.CreateEncryptor(this.aes.Key, this.aes.IV), CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        await swEncrypt.WriteAsync(data);
                    }
                }
                return msEncrypt.ToArray();
            }
        }
        public async Task<string> Decrypt(byte[] data)
        {
            using (MemoryStream msDecrypt = new MemoryStream(data))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, this.aes.CreateDecryptor(this.aes.Key, this.aes.IV), CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return await srDecrypt.ReadToEndAsync();
                    }
                }
            }
        }
    }
}