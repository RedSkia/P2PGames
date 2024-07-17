using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Networking
{
    public interface IReadOnlyServer
    {
        public Encoding Encoding { get; }
        public IPEndPoint EndPoint { get; }
        public IReadOnlyDictionary<byte, TcpClient> Clients { get; }
    }
    public interface ITcpServer : IReadOnlyServer
    {
        public void Start();
        public void Stop();
        public bool Post(string content, byte clientId = 0);
    }
}