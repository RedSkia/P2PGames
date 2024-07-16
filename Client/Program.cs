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
            IEncryptor encryptor = new Encryptor();
            string orginal = "Hello World!";
            string encrypted1 = await encryptor.Encrypt(orginal);
            string decrypt1 = await encryptor.Decrypt(encrypted1);

            string encrypted2 = await encryptor.Encrypt(orginal);
            string decrypt2 = await encryptor.Decrypt(encrypted2);

            Console.WriteLine($"Original: {orginal}");
            Console.WriteLine();

            Console.WriteLine($"IV: {encryptor.IV}");
            Console.WriteLine($"Encypted1: {encrypted1}");
            Console.WriteLine($"Decrypt1: {decrypt1}");
            Console.WriteLine();

            Console.WriteLine($"IV: {encryptor.IV}");
            Console.WriteLine($"Encypted2: {encrypted2}");
            Console.WriteLine($"Decrypt2: {decrypt2}");
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
