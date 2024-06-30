using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{

    public sealed class Server(ushort port, byte maxClients = 2, ushort delayMS = 1000)
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Any, port);
        private readonly Dictionary<byte, TcpClient> clients = new Dictionary<byte, TcpClient>();
        public IReadOnlyDictionary<byte, TcpClient> Clients => this.clients;
        private volatile bool serverRunning = false;
        public event EventHandler<string>? OnClientRead;
        public event EventHandler<string>? OnClientWrite;
        public event EventHandler<string>? OnServerLog;

        public async Task Start()
        {
            try
            {
                this.OnServerLog?.Invoke(this, $"TcpListener running on: {this.listener.LocalEndpoint} awaiting {maxClients} clients...");
                this.listener.Start();
                while (this.clients.Count < maxClients)
                {
                    this.OnServerLog?.Invoke(this, "Listening...");
                    TcpClient client = await this.listener.AcceptTcpClientAsync();
                    byte clientId = (byte)(this.clients.Count + 1);
                    string? IPv4 = (client?.Client?.RemoteEndPoint as IPEndPoint)?.Address?.ToString();
                    this.OnServerLog?.Invoke(this, $"TcpClient #{clientId} connection attempt from IP: {IPv4}");
                    if (client is not null && !this.clients.TryAdd(clientId, client))
                    {
                        this.OnServerLog?.Invoke(this, $"TcpClient #{clientId} failed to connect from IP: {IPv4}");
                        continue;
                    }
                    this.OnServerLog?.Invoke(this, $"TcpClient #{clientId} connected from IP: {IPv4}");
                    await Task.Delay(delayMS);
                }
                this.listener.Stop();
                this.OnServerLog?.Invoke(this, $"TcpListener stopped maximum clients reached! ({this.clients.Keys.Count}/{maxClients})");
                await RunServer();
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"TcpListener failed to listen: {ex.Message}");
            }
        }
        public void Stop()
        {
            try
            {
                this.listener.Stop();
                this.OnServerLog?.Invoke(this, "TcpListener killed!");
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"TcpListener failed to kill: {ex.Message}");
            }
        }
        public async Task<bool> ClientWrite(byte clientId, string message, Encoding? encoding = null)
        {
            if (!this.clients.TryGetValue(clientId, out TcpClient? client) || !client.Connected) return false;
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    encoding ??= Encoding.UTF8;
                    byte[] data = encoding.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                    this.OnClientWrite?.Invoke(client, message);
                }
                return true;
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"Exception writing to client {clientId}: {ex.Message}");
                return false;
            }
        }

        private async Task ClientRead(byte clientId, Encoding? encoding = null)
        {
            if (!this.clients.TryGetValue(clientId, out TcpClient? client)) return;
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead;

                    while (client.Connected && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        encoding ??= Encoding.UTF8;
                        string message = encoding.GetString(buffer, 0, bytesRead);
                        this.OnClientRead?.Invoke(client, message);
                    }
                    await stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"Exception reading from client #{clientId}: {ex.Message}");
            }
        }

        private async Task RunServer()
        {
            if (this.serverRunning)
            {
                this.OnServerLog?.Invoke(this, $"ClientServer multi-instance not allowed!");
                return;
            }
            this.serverRunning = true;
            this.OnServerLog?.Invoke(this, $"ClientServer running!");
            while (true)
            {
                this.OnServerLog?.Invoke(this, $"Server respond!");
                foreach (byte clientId in this.clients.Keys)
                {
                    if (!this.clients[clientId].Connected)
                    {
                        this.OnServerLog?.Invoke(this, $"Client {clientId} disconnected unexpectedly");
                        this.clients.Remove(clientId);
                        continue;
                    }
                    Thread thread = new Thread(async () =>
                    {
                        await ClientRead(clientId);
                    });
                    thread.Start();
                }
                await Task.Delay(delayMS);
            }
        }

        private void KillServer()
        {
            this.serverRunning = false;
        }
    }
}