using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    public sealed class Server(ushort port)
    {
        public event EventHandler<string> OnServerEvent;
        public event EventHandler<string> OnReceiveFromClient;

        private volatile bool isServerRunning = false;
        private readonly TcpListener tcpListener = new TcpListener(IPAddress.Any, port);
        private TcpClient tcpClient;
        public void Start()
        {
            this.tcpListener.Start();
            this.OnServerEvent(this, String.Format("Awaiting Client!"));
            this.isServerRunning = true;
            Thread serverThread = new Thread(async () =>
            {
                while (this.isServerRunning)
                {
                    this.tcpClient = await tcpListener.AcceptTcpClientAsync();
                    this.OnServerEvent(this, String.Format("Client Connected!"));
                }
            });
            serverThread.Start();
        }
        public void Stop()
        {
            this.isServerRunning = false;
            this.tcpListener.Stop();
            this.tcpClient.Close();
        }

        private async void ReceiveFromClient()
        {
            if (!this.isServerRunning || !this.tcpClient.Connected)
            {
                this.OnServerEvent(this, "Cannot read from client!");
                return;
            }
            byte[] buffer = new byte[256];
            int bytesRead;
            while ((bytesRead = await this.tcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                byte[] data = new byte[bytesRead];
                Array.Copy(buffer, data, bytesRead);
                this.OnReceiveFromClient.Invoke(this, Encoding.ASCII.GetString(data));
            }
        }

        private async void SendToClient(byte[] data)
        {
            if(!this.isServerRunning || !this.tcpClient.Connected)
            {
                this.OnServerEvent(this, "Cannot send to client!");
                return;
            }
            await this.tcpClient.GetStream().WriteAsync(data, 0, data.Length);
        }
    }
}
