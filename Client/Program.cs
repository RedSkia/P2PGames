using Networking;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Networking
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IEncryptor encryptor = new Encryptor("GtLNJ1YrTqeHIvqOt42zaf220KQaQ8X0ARk6cuA+ljs=", "hm9xWUJXb0RzGXEgKaNZEg==");
            Console.WriteLine($"KEY: {encryptor.Key}");
            Console.WriteLine($"IV: {encryptor.IV}");
            string content = "Hello World!";
            Console.WriteLine($"ORGINAL: {content}");
            var encrypted = await encryptor.Encrypt(content);
            var decrypted = await encryptor.Decrypt(encrypted);
            Console.WriteLine($"ENCRYPTED: {Encoding.UTF8.GetString(encrypted)}");
            Console.WriteLine($"DECRYPTED: {decrypted}");

            return;
            var c = new Client();
            Console.WriteLine("Connect?");
            Console.ReadKey();
            var client = new TcpClient();
            await client.ConnectAsync("localhost", 12345);

            while (true)
            {
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(message);
                }
            }
            Console.ReadKey();

        }
    }
}
