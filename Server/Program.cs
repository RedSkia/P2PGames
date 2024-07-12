using Networking;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Networking
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IServer server = new Server(12345, 1);
            server.OnServerLog += (sender, args) => WriteLog(args.logMessage, args.logLevel);
            server.OnClientRead += (sender, args) => WriteLog(args.logMessage, args.logLevel);
            server.OnClientWrite += (sender, args) => WriteLog(args.logMessage, args.logLevel);
            server.Start();
            while(true)
            {
                Console.ReadKey();
            }
        }

        private static readonly object _lock = new object();
        private static void WriteLog(string logMessage, string logLevel)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\r[");
                switch (logLevel.ToLower())
                {
                    case "i":
                    case "info":
                        logLevel = "Info";
                        Console.ForegroundColor = ConsoleColor.Cyan; break;
                    case "w":
                    case "warn":
                    case "warning":
                        logLevel = "Warn";
                        Console.ForegroundColor = ConsoleColor.Yellow; break;
                    case "e":
                    case "err":
                    case "error":
                        logLevel = "Error";
                        Console.ForegroundColor = ConsoleColor.Red; break;
                }
                Console.Write(logLevel);
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(']');

                string msgStr = $"{new string(' ', Math.Max(0, 3 - logLevel.Length + 2))}{logMessage}\n\r";
                Console.Write(msgStr);
                Console.ResetColor();
            }
        }
    }
}
