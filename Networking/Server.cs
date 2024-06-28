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
    public sealed class Server(ushort port, byte maxClients = 2)
    {
        private readonly TcpListener listener = new TcpListener(IPAddress.Any, port);
        private readonly Dictionary<byte, TcpClient> clients = new Dictionary<byte, TcpClient>();
        public event EventHandler<string>? OnClientRead;
        public event EventHandler<string>? OnClientWrite;
        public event EventHandler<string>? OnServerLog;

        public async Task StartListener()
        {
            try
            {
                this.listener.Start();
                this.OnServerLog?.Invoke(this, $"TcpListener running on: {port} awaiting {maxClients} clients...");

                Thread clientThread = new Thread(async () =>
                {
                    while (this.clients.Count < maxClients)
                    {
                        TcpClient client = await this.listener.AcceptTcpClientAsync();
                        byte clientId = (byte)(this.clients.Count + 1);
                        this.clients[clientId] = client;
                        this.OnServerLog?.Invoke(this, $"TcpClient #{clientId} connected from IP: {(client.Client.RemoteEndPoint as IPEndPoint)?.Address}");
                        _ = HandleClient(clientId);
                    }
                });
                clientThread.Start();
                await KeepClientsAlive();
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"TcpListener failed to listen: {ex.Message}");
            }
            finally
            {
                this.listener.Stop();
                this.OnServerLog?.Invoke(this, "TcpListener stopped maximum clients reached!");
            }
        }
        public async Task ClientRead(byte clientId, Encoding? encoding = null)
        {
            if (!this.clients.TryGetValue(clientId, out TcpClient? client)) 
                return;
            encoding ??= Encoding.UTF8;
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead;

                    while (client.Connected && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        string message = encoding.GetString(buffer, 0, bytesRead);
                        this.OnClientRead?.Invoke(client, message);
                    }
                }
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"Exception reading from client {clientId}: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public async Task ClientWrite(byte clientId, string message, Encoding? encoding = null)
        {
            if (!this.clients.TryGetValue(clientId, out TcpClient? client) || !client.Connected) 
                return;
            encoding ??= Encoding.UTF8;
            try
            {

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] data = encoding.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);
                    await stream.FlushAsync();
                    this.OnClientWrite?.Invoke(client, message);
                }
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"Exception writing to client {clientId}: {ex.Message}");

            }
        }

        private async Task HandleClient(byte clientId)
        {
            if (!this.clients.TryGetValue(clientId, out TcpClient? client))
                return;
            try
            {
                await ClientRead(clientId);
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"Exception handling client {clientId}: {ex.Message}");
            }
            finally
            {
                if (client.Connected)
                {
                    client.Close();
                    this.OnServerLog?.Invoke(this, $"Client {clientId} disconnected");
                    this.clients.Remove(clientId);
                }
            }
        }

        private async Task KeepClientsAlive()
        {
            try
            {
                while (true)
                {
                    foreach (var clientId in this.clients.Keys)
                    {
                        if (!this.clients[clientId].Connected)
                        {
                            this.OnServerLog?.Invoke(this, $"Client {clientId} disconnected unexpectedly");
                            this.clients.Remove(clientId);
                            _ = HandleClient(clientId); 
                        }
                    }

                    await Task.Delay(5000); 
                }
            }
            catch (Exception ex)
            {
                this.OnServerLog?.Invoke(this, $"Exception in KeepClientsAlive: {ex.Message}");
            }
        }
    }
}