using System.Net;
using System.Runtime.InteropServices;

namespace Networking
{
    static class ConsoleHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        private const int STD_INPUT_HANDLE = -10;
        private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        private const uint SC_SIZE = 0xF000;
        private const uint SC_MINIMIZE = 0xF020;
        private const uint SC_MAXIMIZE = 0xF030;
        private const uint MF_BYCOMMAND = 0x00000000;

        private static void DisableQuickEdit()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);

            // get current console mode
            uint consoleMode;
            if (!GetConsoleMode(consoleHandle, out consoleMode))
            {
                // Handle error
                return;
            }

            // Clear the quick edit bit in the mode flags
            consoleMode &= ~ENABLE_QUICK_EDIT_MODE;
            // Set the extended flags bit
            consoleMode |= ENABLE_EXTENDED_FLAGS;

            if (!SetConsoleMode(consoleHandle, consoleMode))
            {
                // Handle error
                return;
            }
        }

        private static void DisableResize()
        {
            IntPtr consoleWindow = GetConsoleWindow();
            IntPtr systemMenu = GetSystemMenu(consoleWindow, false);

            if (systemMenu != IntPtr.Zero)
            {
                DeleteMenu(systemMenu, SC_SIZE, MF_BYCOMMAND);
            }
        }

        private static void DisableButtons()
        {
            IntPtr consoleWindow = GetConsoleWindow();
            IntPtr systemMenu = GetSystemMenu(consoleWindow, false);

            if (systemMenu != IntPtr.Zero)
            {
                DeleteMenu(systemMenu, SC_MINIMIZE, MF_BYCOMMAND);
                DeleteMenu(systemMenu, SC_MAXIMIZE, MF_BYCOMMAND);
            }
        }

        public static void Setup()
        {
            DisableQuickEdit();
            DisableResize();
            DisableButtons();
            Console.Title = "Server";
            Console.CursorVisible = false;
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
        }
    }


    internal class Program
    {
        static void Main(string[] args)
        {
            ConsoleHelper.Setup();
            Server server = new Server(12345, 1);
            server.Post("dadada", 1);
            #region Events
            server.OnConnection += (sender, args) => WriteLog($"Client Connected: {(args.Client.LocalEndPoint as IPEndPoint)?.Address}", "Advert");
            server.OnDisconnect += (sender, args) => WriteLog($"Client Disconnected: {(args.Client.LocalEndPoint as IPEndPoint)?.Address}", "Advert");
            server.OnReceive += (sender, args) => WriteLog($"Server Receive: client#{args.clientId} {args.content}", "Advert");
            server.OnTransmit += (sender, args) => WriteLog($"Server Transmit: client#{args.clientId} {args.content}", "Advert");
            server.OnStartup += (sender, args) => WriteLog($"Server startup", "Advert");
            server.OnShutdown += (sender, args) => WriteLog($"Server shutdown", "Advert");
           // server.OnReady += (sender, args) => WriteLog($"Server Ready", "Advert");
            server.OnLog += (sender, args) => WriteLog(args.logMessage, args.logLevel);
            #endregion
            server.Start();
            while(true)
            {
                Console.ReadKey(true);
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
                    case "a":
                    case "ad":
                    case "advert":
                        logLevel = "Advert";
                        Console.ForegroundColor = ConsoleColor.Green; break;
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

                Console.ForegroundColor = ConsoleColor.Gray;
                string msgStr = $" {logMessage}\n\r";
                Console.Write(msgStr);
                Console.ResetColor();
            }
        }
    }
}