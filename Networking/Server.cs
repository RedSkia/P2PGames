using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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
        private volatile bool serverRunning = false;
        public event EventHandler<string>? OnClientRead;
        public event EventHandler<string>? OnClientWrite;
        public event EventHandler<string>? OnServerLog;
        public void Start()
        {
            try
            {
                this.listener.Start();
                this.OnServerLog?.Invoke(this, $"TcpListener running on: {port} awaiting {maxClients} clients...");
                Thread listenerThread = new Thread(async () =>
                {
                    while (this.clients.Count < maxClients)
                    {
                        TcpClient client = await this.listener.AcceptTcpClientAsync();
                        byte clientId = (byte)(this.clients.Count + 1);
                        if (!this.clients.TryAdd(clientId, client))
                        {
                            this.OnServerLog?.Invoke(this, $"TcpClient #{clientId} failed to connect from IP: {(client.Client.RemoteEndPoint as IPEndPoint)?.Address}");
                            continue;
                        }
                        this.OnServerLog?.Invoke(this, $"TcpClient #{clientId} connected from IP: {(client.Client.RemoteEndPoint as IPEndPoint)?.Address}");
                        await Task.Delay(delayMS);
                    }
                });
                listenerThread.Start();
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"TcpListener failed to listen: {ex.Message}");
            }
            finally
            {
                this.listener.Stop();
                this.OnServerLog?.Invoke(this, $"TcpListener stopped maximum clients reached! ({this.clients.Keys.Count}/{maxClients})");
                RunServer();
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

        private void RunServer()
        {
            if(this.serverRunning)
            {
                this.OnServerLog?.Invoke(this, $"ClientServer multi-instance not allowed!");
                return;
            }
            Thread serverThread = new Thread(async () =>
            {
                while(this.serverRunning)
                {
                    foreach (byte clientId in this.clients.Keys)
                    {
                        if (!this.clients[clientId].Connected)
                        {
                            this.OnServerLog?.Invoke(this, $"Client {clientId} disconnected unexpectedly");
                            this.clients.Remove(clientId);
                            _ = ClientRead(clientId);
                        }
                    }

                    await Task.Delay(delayMS);
                }
            });
            serverThread.Start();
        }
        private void KillServer()
        {
            this.serverRunning = false;
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
                }
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"Exception reading from client {clientId}: {ex.Message}");
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
                    return true;
                }
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"Exception writing to client {clientId}: {ex.Message}");
                return false;
            }
        }
    }
}