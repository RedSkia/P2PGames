using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    public static class Authentication
    {
        public static async Task<bool> Authenticate(TcpClient client, string token, Encoding? encoding = null, ushort timeoutMS = 1000)
        {
            try
            {
                CancellationTokenSource cancellation = new CancellationTokenSource();
                cancellation.CancelAfter(timeoutMS);

                byte[] buffer = new byte[client.ReceiveBufferSize];
                Task<int> readTask = client.GetStream().ReadAsync(buffer, 0, buffer.Length, cancellation.Token);
                await Task.WhenAny(readTask, Task.Delay(timeoutMS, cancellation.Token));
                if (!readTask.IsCompleted) return false; /*timeout*/

                string credentials = (encoding ??= Encoding.UTF8).GetString(buffer, 0, await readTask);
                return String.Equals(credentials, token, StringComparison.Ordinal);
            }
            catch { return false; }
        }
    }
}