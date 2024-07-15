using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    public sealed class Server(ushort port, byte maxClients = 2, ushort tickInterval = 1000) : NetworkingEvents<(string logMessage, string logLevel)>, IServer
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Any, port);
        private readonly ConcurrentDictionary<byte, TcpClient> clients = new ConcurrentDictionary<byte, TcpClient>();
        private volatile bool running = false;
        private IPEndPoint endpoint => (this.listener.LocalEndpoint as IPEndPoint)!;
        public Encoding Encoding => Encoding.UTF8;
        public IPEndPoint EndPoint => this.endpoint;
        public IReadOnlyDictionary<byte, TcpClient> Clients => this.clients;
        #region Events
        public override event EventHandler<(byte clientId, string content)>? OnReceive;
        public override event EventHandler<(byte clientId, string content)>? OnTransmit;
        public override event EventHandler<TcpClient>? OnConnection;
        public override event EventHandler<TcpClient>? OnDisconnect;
        public override event EventHandler? OnStartup;
        public override event EventHandler? OnShutdown;
        public override event EventHandler? OnTick;
        public override event EventHandler<(string logMessage, string logLevel)>? OnLog;
        #endregion
        public async void Start()
        {
            try
            {
                this.OnStartup?.Invoke(this, EventArgs.Empty);
                var cancellation = new CancellationTokenSource();
                this.OnLog?.Invoke(this, ($"Listener starting...", "Info"));
                await Task.Factory.StartNew(async () =>
                {
                    this.OnLog?.Invoke(this, ($"Listener running", "Info"));
                    this.OnLog?.Invoke(this, ($"TcpListener starting...", "Info"));
                    this.listener.Start();
                    this.OnLog?.Invoke(this, ($"TcpListener running", "Info"));
                    this.OnLog?.Invoke(this, ($"TcpListener available @ {this.endpoint?.Address}:{this.endpoint?.Port}", "Info"));
                    while (this.Clients.Count < maxClients)
                    {
                        this.OnLog?.Invoke(this, ("TcpListener listening for clients...", "Info"));
                        TcpClient client = await this.listener.AcceptTcpClientAsync();
                        byte clientId = (byte)(this.Clients.Count + 1);
                        string? clientIp = (client?.Client?.RemoteEndPoint as IPEndPoint)?.Address?.ToString();
                        this.OnLog?.Invoke(this, ($"TcpClient #{clientId} connecting... @ {clientIp}", "Info"));
                        if (client is null || !this.clients.TryAdd(clientId, client))
                        {
                            this.OnLog?.Invoke(this, ($"TcpClient #{clientId} failed to connect @ {clientIp}", "Warn"));
                            continue;
                        }
                        this.OnLog?.Invoke(this, ($"TcpClient #{clientId} connected @ {clientIp}", "Info"));
                        this.OnConnection?.Invoke(this, client);
                        await Task.Delay(tickInterval);
                    }
                    this.OnLog?.Invoke(this, ($"TcpListener maximum clients reached! ({this.Clients.Keys.Count()}/{maxClients}) stopping...", "Info"));
                    this.listener.Stop();
                    this.OnLog?.Invoke(this, ($"TcpListener stopped", "Info"));
                }).WaitAsync(cancellation.Token);
                this.OnLog?.Invoke(this, ($"Listener ended", "Info"));
            }
            catch (Exception ex)
            {
                this.OnLog?.Invoke(this, ($"Listener thread failed to start: {ex.Message}", "Error"));
            }
        }
        public void Stop()
        {
            this.OnLog?.Invoke(this, ($"Server stopping...", "Info"));
            if (!this.running)
            {
                this.OnLog?.Invoke(this, ($"Server cannot stop non-running instance", "Warn"));
                return;
            }
            this.OnLog?.Invoke(this, ($"Server status stopping...", "Info"));
            this.running = false;
            this.OnLog?.Invoke(this, ($"Server status offline", "Info"));
            this.OnLog?.Invoke(this, ($"Server listener stopping...", "Info"));
            this.listener.Stop();
            this.OnLog?.Invoke(this, ($"Server listerner stopped", "Info"));
            this.OnLog?.Invoke(this, ($"Server clients disconnecting...", "Info"));
            foreach (var client in this.Clients)
            {
                this.OnLog?.Invoke(this, ($"Client #{client.Key} disconnecting...", "Info"));
                client.Value.Close();
                this.OnDisconnect?.Invoke(this, client.Value);
                this.OnLog?.Invoke(this, ($"Client #{client.Key} disconnected", "Info"));
                this.OnLog?.Invoke(this, ($"Client #{client.Key} removing...", "Info"));
               // this.clients.Remove(client.Key);
                this.OnLog?.Invoke(this, ($"Client #{client.Key} removed", "Info"));
            }
            this.OnLog?.Invoke(this, ($"Server clients disconnected", "Info"));
            this.OnShutdown?.Invoke(this, EventArgs.Empty);
        }
        public bool Post(string content, byte clientId = 0)
        {
            if (clientId > 0) return this.ClientTransmit(clientId, content).Result;
            else
            {
                foreach (var client in this.Clients)
                    if(!this.ClientTransmit(client.Key, content).Result) { return false; }
                return true;
            }
        }
        private void KeepServerAlive()
        {
            if(this.running)
            {
                this.OnLog?.Invoke(this, ($"Server thread already running! multi-instance not allowed", "Warn"));
                return;
            }
            Thread serverThread = new Thread(async () =>
            {
                this.running = true;
                //this.OnReady?.Invoke(this, EventArgs.Empty);
                this.OnLog?.Invoke(this, ($"Server thread running", "Info"));
                while (this.running)
                {
                    if(this.Clients.Count() != maxClients)
                    {
                        this.OnLog?.Invoke(this, ($"Server not satisfied with enough clients ({this.Clients.Keys.Count()}/{maxClients})!", "Warn"));
                        break;
                    }
                    this.OnLog?.Invoke(this, ($"Server alive!", "Info"));
                    foreach (byte clientId in this.Clients.Keys)
                    {
                        if (!this.Clients[clientId].Connected)
                        {
                            this.OnLog?.Invoke(this, ($"Client #{clientId} disconnected unexpectedly", "Warn"));
                            this.OnDisconnect?.Invoke(this, this.Clients[clientId]);
                            this.clients.TryRemove(clientId, out _);
                            continue;
                        }
                        await ClientReceive(clientId);
                    }
                    //await Task.Delay(delayMS);
                }
                this.OnLog?.Invoke(this, ($"Server thread ended", "Info"));
                this.OnLog?.Invoke(this, ($"Server re-starting...", "Warn"));
                this.Stop();
                this.Start();
            });
            this.OnLog?.Invoke(this, ($"Server thread starting...", "Info"));
            serverThread.Start();
        }
        private async Task<bool> ClientTransmit(byte clientId, string message)
        {
            if (!this.Clients.TryGetValue(clientId, out TcpClient? client) || !client.Connected)
            {
                this.OnLog?.Invoke(this, ($"Client #{clientId} not found/connected", "Warn"));
                return false;
            }
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] data = this.Encoding.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
                this.OnLog?.Invoke(this, (message, "Info"));
                this.OnTransmit?.Invoke(this, (clientId, message));
                return true;
            }
            catch (Exception ex)
            {
                this.OnLog?.Invoke(this, ($"Exception client #{clientId} failed to transmit: {ex.Message}", "Error"));
                return false;
            }
        }
        private async Task<bool> ClientReceive(byte clientId)
        {
            if (!this.Clients.TryGetValue(clientId, out TcpClient? client))
            {
                this.OnLog?.Invoke(this, ($"Client #{clientId} not found/connected", "Warn"));
                return false;
            }
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[client.ReceiveBufferSize];
                int bytesRead;

                while (client.Connected && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string message = this.Encoding.GetString(buffer, 0, bytesRead);
                    this.OnLog?.Invoke(client, (message, "Info"));
                    this.OnReceive?.Invoke(this, (clientId, message));
                }
                await stream.FlushAsync();
                return true;
            }
            catch (Exception ex)
            {
                this.OnLog?.Invoke(client, ($"Exception client #{clientId} failed to receive: {ex.Message}", "Error"));
                return false;
            }
        }
    }
}