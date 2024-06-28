using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public static class Helper
    {
        public static ushort GetFreePort(ushort startPort = 1024, ushort endPort = ushort.MaxValue)
        {
            IPEndPoint[] activeTcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return (ushort)(activeTcpListeners.Where(p => p.Port > startPort && p.Port < endPort)?.SingleOrDefault()?.Port ?? 0);
        }
    }
}
