using System;
using System.Net.Sockets;

namespace Networking
{
    public abstract class NetworkingEvents<TEventArgs>
    {
       public virtual event EventHandler<(byte clientId, string content)>? OnReceive;
       public virtual event EventHandler<(byte clientId, string content)>? OnTransmit;
       public virtual event EventHandler<TcpClient>? OnConnection;
       public virtual event EventHandler<TcpClient>? OnDisconnect;
       public virtual event EventHandler? OnStartup;
       public virtual event EventHandler? OnShutdown;
       public virtual event EventHandler? OnTick;
       public virtual event EventHandler<TEventArgs>? OnLog;
    }
}