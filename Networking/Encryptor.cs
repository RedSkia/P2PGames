using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Networking
{
    public interface IEncryptor
    {
        public string Key { get; }
        public string IV { get; }
        public Task<byte[]> Encrypt(string data);
        public Task<string> Decrypt(byte[] data);
    }
    public sealed class Encryptor : IEncryptor, IDisposable
    {
        private readonly Aes aes = Aes.Create();
        public string Key => Convert.ToBase64String(this.aes.Key);
        public string IV => Convert.ToBase64String(this.aes.IV);
        public Encryptor(string? key = null, string? iv = null)
        {
            this.aes.KeySize = 256;
            this.aes.BlockSize = 128;
            this.aes.FeedbackSize = 128;
            this.aes.Mode = CipherMode.CBC;
            this.aes.Padding = PaddingMode.PKCS7;
            if(key is null || iv is null || !ValidateKeys())
            {
                this.aes.GenerateKey();
                this.aes.GenerateIV();
            }
            bool ValidateKeys()
            {
                try
                {
                    byte[] byteKey = Convert.FromBase64String(key ?? String.Empty);
                    byte[] byteIv = Convert.FromBase64String(iv ?? String.Empty);
                    if (byteKey.Length != (this.aes.KeySize / 8) || byteIv.Length is not 16) return false;
                    this.aes.Key = byteKey;
                    this.aes.IV = byteIv;
                    return true;
                }
                catch { return false; }
            }
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
        public void Dispose()
        {
            this.aes.Key = default!;
            this.aes.IV = default!;
            this.aes.Clear();
        }
        ~Encryptor() => this.Dispose();
    }
}