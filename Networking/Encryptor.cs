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
        public Task<string> Encrypt(string data);
        public Task<string> Decrypt(string data);
    }
    public sealed class Encryptor : IEncryptor, IDisposable
    {
        private readonly Aes aes = Aes.Create();
        public string Key => Convert.ToBase64String(this.aes.Key);
        public string IV => Convert.ToBase64String(this.aes.IV);
        public Encryptor(string? key = null)
        {
            this.aes.KeySize = 256;
            this.aes.BlockSize = 128;
            this.aes.FeedbackSize = 128;
            this.aes.Mode = CipherMode.CBC;
            this.aes.Padding = PaddingMode.PKCS7;
            if(key is null || !ApplyKey()) this.aes.GenerateKey();
            bool ApplyKey()
            {
                try
                {
                    byte[] byteKey = Convert.FromBase64String(key ?? String.Empty);
                    if (byteKey.Length != (this.aes.KeySize / 8)) return false;
                    this.aes.Key = byteKey;
                    return true;
                }
                catch { return false; }
            }
        }
        public async Task<string> Encrypt(string data)
        {
            this.aes.GenerateIV();
            byte[] iv = this.aes.IV;
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, this.aes.CreateEncryptor(this.aes.Key, iv), CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) 
                    await swEncrypt.WriteAsync(data);

                byte[] encrypted = msEncrypt.ToArray();
                byte[] result = new byte[iv.Length + encrypted.Length];
                Array.Copy(iv, 0, result, 0, iv.Length);
                Array.Copy(encrypted, 0, result, iv.Length, encrypted.Length);
                return Convert.ToBase64String(result);
            }
        }
        public async Task<string> Decrypt(string data)
        {
            byte[] fullCipher = Convert.FromBase64String(data);
            byte[] iv = new byte[this.aes.BlockSize / 8];
            byte[] encrypted = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            Array.Copy(fullCipher, iv.Length, encrypted, 0, encrypted.Length);

            using (MemoryStream msDecrypt = new MemoryStream(encrypted))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, this.aes.CreateDecryptor(this.aes.Key, iv), CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt)) 
                    return await srDecrypt.ReadToEndAsync();
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