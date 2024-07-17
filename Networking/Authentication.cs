using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    public readonly struct AuthenticationToken() : IEquatable<AuthenticationToken>
    {
        public readonly string Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        public AuthenticationToken(string token) : this()
        {
            try
            {
                byte[] bytesToken = Convert.FromBase64String(token);
                if (bytesToken.Length == 16) this.Token = token;
            }
            catch { }
        }
        public bool Equals(AuthenticationToken other)
        {
            byte[] thisBytes = Convert.FromBase64String(this.Token);
            byte[] otherBytes = Convert.FromBase64String(other.Token);
            return thisBytes.AsSpan().SequenceEqual(otherBytes);
        }
        public override bool Equals(object? obj)
        {
            if (obj is AuthenticationToken otherToken) return this.Equals(otherToken);
            return false;
        }
        public override int GetHashCode() => HashCode.Combine(this.Token);
        public static implicit operator string(AuthenticationToken token) => token.Token;
        public static implicit operator AuthenticationToken(string token) => new AuthenticationToken(token);
        public static bool operator ==(AuthenticationToken left, AuthenticationToken right) => left.Equals(right);
        public static bool operator !=(AuthenticationToken left, AuthenticationToken right) => !left.Equals(right);
    }
    public static class Authentication
    {
        public static async Task<bool> Authenticate(TcpClient client, AuthenticationToken token, ushort timeoutMS = 1000)
        {
            try
            {
                if (client is null || client.Connected is false) return false;

                CancellationTokenSource cancellation = new CancellationTokenSource();
                cancellation.CancelAfter(timeoutMS);

                byte[] buffer = new byte[client.ReceiveBufferSize];
                Task<int> readTask = client.Client.ReceiveAsync(buffer);
                await Task.WhenAny(readTask, Task.Delay(timeoutMS, cancellation.Token));
                if (!readTask.IsCompleted) return false;
                int readBytes = await readTask;
                string credentials = Convert.ToBase64String(buffer, 0, readBytes);

                return token.Equals(credentials);
            }
            catch { return false; }
        }
    }
}