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
    public sealed class Server(ushort port, byte maxClients = 2, ushort delayMS = 1000) : IServer
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Any, port);
        private readonly Dictionary<byte, TcpClient> clients = new Dictionary<byte, TcpClient>();
        private volatile bool running = false;
        private IPEndPoint? endpoint => (this.listener.LocalEndpoint as IPEndPoint);
        public IReadOnlyDictionary<byte, TcpClient> Clients => this.clients;
        public event EventHandler<(string logMessage, string logLevel)>? OnClientRead;
        public event EventHandler<(string logMessage, string logLevel)>? OnClientWrite;
        public event EventHandler<(string logMessage, string logLevel)>? OnServerLog;
        public void Start()
        {
            try
            {
                Thread listenerThread = new Thread(async () =>
                {
                    this.OnServerLog?.Invoke(this, ($"Listener thread running", "Info"));
                    this.OnServerLog?.Invoke(this, ($"TcpListener starting...", "Info"));
                    this.listener.Start();
                    this.OnServerLog?.Invoke(this, ($"TcpListener running", "Info"));
                    this.OnServerLog?.Invoke(this, ($"TcpListener available @ {this.endpoint?.Address}:{this.endpoint?.Port}", "Info"));
                    while (this.Clients.Count < maxClients)
                    {
                        this.OnServerLog?.Invoke(this, ("Listening for clients...", "Info"));
                        TcpClient client = await this.listener.AcceptTcpClientAsync();
                        byte clientId = (byte)(this.Clients.Count + 1);
                        string? IPv4 = (client?.Client?.RemoteEndPoint as IPEndPoint)?.Address?.ToString();
                        this.OnServerLog?.Invoke(this, ($"TcpClient #{clientId} connecting... @ {IPv4}", "Info"));
                        if (client is not null && !this.clients.TryAdd(clientId, client))
                        {
                            this.OnServerLog?.Invoke(this, ($"TcpClient #{clientId} failed to connect @ {IPv4}", "Warn"));
                            continue;
                        }
                        this.OnServerLog?.Invoke(this, ($"TcpClient #{clientId} connected @ {IPv4}", "Info"));
                        await Task.Delay(delayMS);
                    }
                    this.OnServerLog?.Invoke(this, ($"TcpListener stopping maximum clients reached! ({this.Clients.Keys.Count()}/{maxClients})...", "Info"));
                    this.listener.Stop();
                    this.OnServerLog?.Invoke(this, ($"TcpListener stopped", "Info"));
                    KeepServerAlive();
                });
                this.OnServerLog?.Invoke(this, ($"Starting listener thread...", "Info"));
                listenerThread.Start();
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, ($"Failed to start listener thread: {ex.Message}", "Error"));
            }
        }
        public void Stop()
        {
            this.OnServerLog?.Invoke(this, ($"Stopping server...", "Info"));
            if (!this.running)
            {
                this.OnServerLog?.Invoke(this, ($"Cannot stop non-running server", "Warn"));
                return;
            }
            this.OnServerLog?.Invoke(this, ($"Stopping server status...", "Info"));
            this.running = false;
            this.OnServerLog?.Invoke(this, ($"Server status offline", "Info"));
            this.OnServerLog?.Invoke(this, ($"Stopping listener...", "Info"));
            this.listener.Stop();
            this.OnServerLog?.Invoke(this, ($"Listerner stopped", "Info"));
            this.OnServerLog?.Invoke(this, ($"Clearing client connections...", "Info"));
            foreach (var client in this.Clients)
            {
                this.OnServerLog?.Invoke(this, ($"Disconnecting client #{client.Key}...", "Info"));
                client.Value.Close();
                this.OnServerLog?.Invoke(this, ($"Client #{client.Key} disconnected", "Info"));
                this.OnServerLog?.Invoke(this, ($"Removing client #{client.Key}...", "Info"));
                this.clients.Remove(client.Key);
                this.OnServerLog?.Invoke(this, ($"Client #{client.Key} removed", "Info"));
            }
            this.OnServerLog?.Invoke(this, ($"Clients connections cleared", "Info"));
        }
        public bool Broadcast(byte clientId, string message) => ClientWrite(clientId, message).Result;
        private void KeepServerAlive()
        {
            if(this.running)
            {
                this.OnServerLog?.Invoke(this, ($"Server thread already running multi-instance not allowed", "Warn"));
                return;
            }
            Thread serverThread = new Thread(async () =>
            {
                this.OnServerLog?.Invoke(this, ($"Server thread running", "Info"));
                while (this.running)
                {
                    if(this.Clients.Count() != maxClients)
                    {
                        this.OnServerLog?.Invoke(this, ($"Server not satisfied with enough clients ({this.Clients.Keys.Count()}/{maxClients})!", "Warn"));
                        break;
                    }
                    this.OnServerLog?.Invoke(this, ($"Server alive!", "Info"));
                    foreach (byte clientId in this.Clients.Keys)
                    {
                        if (!this.Clients[clientId].Connected)
                        {
                            this.OnServerLog?.Invoke(this, ($"Client #{clientId} disconnected unexpectedly", "Warn"));
                            this.clients.Remove(clientId);
                            continue;
                        }
                        await ClientRead(clientId);
                    }
                    await Task.Delay(delayMS);
                }
                this.OnServerLog?.Invoke(this, ($"Server thread ended", "Info"));
                this.OnServerLog?.Invoke(this, ($"Server re-starting...", "Warn"));
                this.Stop();
                this.Start();
            });
            this.OnServerLog?.Invoke(this, ($"Starting server thread...", "Info"));
            serverThread.Start();
            this.running = true;
        }
        private async Task<bool> ClientWrite(byte clientId, string message, Encoding? encoding = null)
        {
            if (!this.Clients.TryGetValue(clientId, out TcpClient? client) || !client.Connected)
            {
                this.OnServerLog?.Invoke(this, ($"Client #{clientId}: not found or connected", "Warn"));
                return false;
            }
            try
            {
                NetworkStream stream = client.GetStream();
                encoding ??= Encoding.UTF8;
                byte[] data = encoding.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
                this.OnClientWrite?.Invoke(client, (message, "Info"));
                return true;
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, ($"Exception writing to client #{clientId}: {ex.Message}", "Error"));
                return false;
            }
        }
        private async Task<bool> ClientRead(byte clientId, Encoding? encoding = null)
        {
            if (!this.Clients.TryGetValue(clientId, out TcpClient? client))
            {
                this.OnServerLog?.Invoke(this, ($"Client #{clientId}: not found or connected", "Warn"));
                return false;
            }
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead;

                while (client.Connected && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    encoding ??= Encoding.UTF8;
                    string message = encoding.GetString(buffer, 0, bytesRead);
                    this.OnClientRead?.Invoke(client, (message, "Info"));
                }
                await stream.FlushAsync();
                return true;
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(client, ($"Exception reading from client #{clientId}: {ex.Message}", "Error"));
                return false;
            }
        }
    }
}