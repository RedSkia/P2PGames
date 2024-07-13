using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Networking
{
    public interface IServer
    {
        public IReadOnlyDictionary<byte, TcpClient> Clients { get; }
        public void Start();
        public void Stop();
        public bool Broadcast(string message, byte clientId = 0);
    }
}