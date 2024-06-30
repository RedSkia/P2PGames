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
            Server server = new Server(12345, 1);
            server.OnServerLog += (s, log) => Console.WriteLine($"Server Log: {log}");
            server.OnClientRead += (s, msg) => Console.WriteLine($"Server Received: {msg}");
            server.OnClientWrite += (s, msg) => Console.WriteLine($"Server Sent: {msg}");
            await server.Start();
            Console.WriteLine("Write to client?");
            Console.ReadKey();
            await server.ClientWrite(1, "Hello client from server!");
            Console.ReadKey();
        }
    }
}
