using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    public sealed class Server(ushort port)
    {
        public event EventHandler<string> OnServerEvent;
        private TcpListener listener = new TcpListener(IPAddress.Any, port);

        public async void Start()
        {
            this.listener.Start();
            this.OnServerEvent.Invoke(this, $"Tcp listener on port: {port}");
            while(this.listener.Pending())
            {
                await Task.Delay(1000);
                this.OnServerEvent.Invoke(this, $"Listening for connections...");
            }
            TcpClient client = await this.listener.AcceptTcpClientAsync();
            this.OnServerEvent.Invoke(this, $"Client connected!");

            NetworkStream stream = client.GetStream();

        }


    }
}