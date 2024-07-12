using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Networking
{
    public interface IServer
    {
        public IReadOnlyDictionary<byte, TcpClient> Clients { get; }
        public void Start();
        public void Stop();
        public bool Broadcast(byte clientId, string message);
        public event EventHandler<(string logMessage, string logLevel)>? OnClientRead;
        public event EventHandler<(string logMessage, string logLevel)>? OnClientWrite;
        public event EventHandler<(string logMessage, string logLevel)>? OnServerLog;
    }
}