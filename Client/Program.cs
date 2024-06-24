using Networking;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Client
{
    internal class Program
    {
        const int PORT_NO = 7000;
        const string SERVER_IP = "127.0.0.1";

        static void Main(string[] args)
        {
            Console.Title = "CLIENT";

            TcpClient client = new TcpClient(SERVER_IP, PORT_NO);
            Console.WriteLine($"Connected to {SERVER_IP}:{PORT_NO}");

            NetworkStream stream = client.GetStream();

            Thread thread = new Thread(ReceiveData);
            thread.Start(client);

            try
            {

                while (true)
                {
                    string textToSend = Console.ReadLine();

                    byte[] buffer = Encoding.ASCII.GetBytes(textToSend);

                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception)
            {

                client.Client.Shutdown(SocketShutdown.Both);
                thread.Join();
                stream.Close();
                client.Close();
                Console.WriteLine("Disconnected!");
                Console.ReadLine();
            }



        }

        private static void ReceiveData(object o)
        {
            TcpClient client = (TcpClient)o;

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int byte_count;

            while (true)
            {
                byte_count = stream.Read(buffer, 0, buffer.Length);

                string message = Encoding.ASCII.GetString(buffer, 0, byte_count);

                Console.WriteLine(message);
            }


        }
    }
}
