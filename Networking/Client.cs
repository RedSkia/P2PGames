using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public sealed class Client(string serverIp, ushort serverPort)
    {
        private readonly TcpClient client = new TcpClient();
        public event EventHandler<string>? OnClientRead;
        public event EventHandler<string>? OnClientWrite;
        public event EventHandler<string>? OnClientLog;

        public async Task Connect()
        {
            try
            {
                await this.client.ConnectAsync(serverIp, serverPort);
                this.OnClientLog?.Invoke(this, $"Client connected to server {serverIp}:{serverPort}");
            }
            catch (Exception ex)
            {
                this.OnClientLog?.Invoke(this, $"Client failed to connect: {ex.Message}");
            }
        }

        public async Task Write(string message, Encoding? encoding = null)
        {
            if (!this.client.Connected)
            {
                this.OnClientLog?.Invoke(this, "Client not connected to server");
                return;
            }

            encoding ??= Encoding.UTF8;
            byte[] data = encoding.GetBytes(message);

            try
            {
                using (NetworkStream stream = this.client.GetStream())
                {
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                    this.OnClientWrite?.Invoke(this, $"Sent: {message}");
                }
     
            }
            catch (Exception ex)
            {
                this.OnClientLog?.Invoke(this, $"Client failed to send: {ex.Message}");
            }
        }

        public async Task Read(Encoding? encoding = null)
        {
            if (!this.client.Connected)
            {
                this.OnClientLog?.Invoke(this, "Client not connected to server");
                return;
            }

            encoding ??= Encoding.UTF8;
            byte[] buffer = new byte[this.client.ReceiveBufferSize];

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = encoding.GetString(buffer, 0, bytesRead);
                        this.OnClientRead?.Invoke(this, $"Received: {message}");
                    }
                }
      
            }
            catch (Exception ex)
            {
                this.OnClientLog?.Invoke(this, $"Client failed to receive: {ex.Message}");
            }
        }

        public void Close()
        {
            this.client.Close();
        }
    }
}
