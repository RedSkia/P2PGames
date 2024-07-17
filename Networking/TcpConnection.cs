using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    public sealed class TcpConnection : NetworkingEvents<(string logMessage, string logLevel)>, IClient
    {
        private readonly TcpClient client = new TcpClient();
        private volatile bool running = false;
        public async Task<bool> Connect(string host, ushort port)
        {
            if(!IPAddress.TryParse(host, out IPAddress? address) || address is null) return false;
            return await this.Connect(new IPEndPoint(address, port));
        }
        public async Task<bool> Connect(IPEndPoint endPoint)
        {
            try
            {
                await this.client.ConnectAsync(endPoint);
                this.KeepClientAlive();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Disconnect()
        {
            if (this.client!.Connected)
            {
                this.client.Close();
                return true;
            }
            return false;
        }
        public async Task<bool> Post(string content) => await this.Transmit(content);
        private void KeepClientAlive()
        {
            if (this.running)
            {
                return;
            }
            Thread clientThread = new Thread(async () =>
            {
                this.running = true;
                while (this.running)
                {
                    await this.Receive();
                    await Task.Delay(1000);
                }
            });
            clientThread.Start();
        }
        private async Task<bool> Transmit(string content)
        {
            try
            {
                if (this.client is null || !this.client.Connected)
                {
                    return false;
                }
                NetworkStream stream = this.client.GetStream();
                byte[] bytesData = Encoding.UTF8.GetBytes(content);
                await stream.WriteAsync(bytesData, 0, bytesData.Length);
                await stream.FlushAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private async Task Receive()
        {
            try
            {
                if (this.client is null || !this.client.Connected)
                {
                    return;
                }
                NetworkStream stream = this.client.GetStream();
                byte[] buffer = new byte[this.client.ReceiveBufferSize];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string content = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
            }
        }
    }
}