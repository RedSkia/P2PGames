using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Networking
{
    public static class Helper
    {
        public static async Task<string> Serialize<T>(T obj)
        {
            await using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, obj);
            stream.Position = 0;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        public static async Task<T?> Deserialize<T>(this string json, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] bytes = encoding.GetBytes(json);
            await using var stream = new MemoryStream(bytes);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        }
        public static ushort GetFreePort(ushort startPort = 1024, ushort endPort = ushort.MaxValue)
        {
            IPEndPoint[] activeTcpListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
            return (ushort)(activeTcpListeners.Where(p => p.Port > startPort && p.Port < endPort)?.SingleOrDefault()?.Port ?? 0);
        }
    }
}
