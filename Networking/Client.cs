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
        public event EventHandler<(string logMessage, string logLevel)>? OnClientRead;
        public event EventHandler<(string logMessage, string logLevel)>? OnClientWrite;
        public event EventHandler<(string logMessage, string logLevel)>? OnClientLog;

        public async Task Connect()
        {
            try
            {
                this.OnClientLog?.Invoke(this, ($"Client connecting to server...", "Info"));
                await this.client.ConnectAsync(serverIp, serverPort);
                this.OnClientLog?.Invoke(this, ($"Client connected to server {serverIp}:{serverPort}", "Info"));
            }
            catch (Exception ex)
            {
                this.OnClientLog?.Invoke(this, ($"Client failed to connect: {ex.Message}", "Error"));
            }
        }

        public async Task Write(string message, Encoding? encoding = null)
        {
            if (!this.client.Connected)
            {
                this.OnClientLog?.Invoke(this, ("Client cannot write to non-connected server", "Warn"));
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
                    this.OnClientWrite?.Invoke(this, ($"Client sent: {message}", "Info"));
                }
     
            }
            catch (Exception ex)
            {
                this.OnClientLog?.Invoke(this, ($"Client failed to send: {ex.Message}", "Error"));
            }
        }

        public async Task Read(Encoding? encoding = null)
        {
            if (!this.client.Connected)
            {
                this.OnClientLog?.Invoke(this, ("Client cannot read to non-connected server", "Warn"));
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
                        this.OnClientRead?.Invoke(this, ($"Client received: {message}", "Info"));
                    }
                }
      
            }
            catch (Exception ex)
            {
                this.OnClientLog?.Invoke(this, ($"Client failed to receive: {ex.Message}", "Error"));
            }
        }

        public void Close()
        {
            this.client.Close();
        }
    }
}