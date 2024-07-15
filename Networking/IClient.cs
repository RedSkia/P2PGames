using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public interface IClient
    {
        public Task<bool> Connect(string host, ushort port);
        public Task<bool> Connect(IPEndPoint endPoint);
        public bool Disconnect();
        public Task<bool> Post(string content);
    }
}