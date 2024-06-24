using Networking;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Networking
{
    internal class Program
    {
        static object _lock = new object();
        static Dictionary<int, TcpClient> list_client = new Dictionary<int, TcpClient>();
        static void Main(string[] args)
        {
            Console.Title = "SERVER";
            try
            {

                TcpListener server = new TcpListener(IPAddress.Any, 7000);
                server.Start();
                int count = 0;
                Console.WriteLine("Server started!");

                while (true)
                {
                    count++;
                    TcpClient client = server.AcceptTcpClient();

                    lock (_lock) list_client.Add(count, client);

                    Thread thread = new Thread(handle_clients);
                    Console.WriteLine($"We have new user! id#{count}");
                    broadcast($"New user enterd chat id#{count}", count);
                    thread.Start(count);
                }

                server.Stop();
                Console.WriteLine("Server stopped");
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }

        }

        private static void handle_clients(object o)
        {
            int id = (int)o;

            var client = new TcpClient();

            lock (_lock) client = list_client[id];

            NetworkStream stream = client.GetStream();
            while (true)
            {
                if (stream.CanRead)
                {
                    byte[] buffer = new byte[client.ReceiveBufferSize];

                    int bytesCount = 0;
                    try
                    {
                        bytesCount = stream.Read(buffer, 0, client.ReceiveBufferSize);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"user#{id} left");
                        broadcast($"#{id} left", id);
                        lock (_lock) list_client.Remove(id);
                        client.Client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        return;
                    }

                    string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesCount);

                    broadcast($"user#{id}: {dataReceived}", id);
                    Console.WriteLine($"Data from user#{id}: {dataReceived}");
                }
            }
        }

        public static void broadcast(string message, int id)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);

            lock (_lock)
            {
                foreach (var client in list_client)
                {
                    if (client.Key != id)
                    {
                        NetworkStream stream = client.Value.GetStream();
                        stream.Write(buffer, 0, buffer.Length);
                    }
                }
            }


        }

    }
}
