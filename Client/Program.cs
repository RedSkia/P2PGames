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
            Client client = new Client("localhost", 12345);
            client.OnClientLog += (s, log) => Console.WriteLine($"Client Log: {log}");
            client.OnClientRead += (s, msg) => Console.WriteLine($"Client Received: {msg}");
            client.OnClientWrite += (s, msg) => Console.WriteLine($"Client Sent: {msg}");
            await client.Connect();
            while (true)
            {
                await client.Write(Console.ReadLine());
            }
            Console.ReadKey();
        }
    }
}
