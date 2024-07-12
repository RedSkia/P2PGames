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
