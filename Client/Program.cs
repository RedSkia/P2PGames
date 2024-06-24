using Networking;
using System.Security.Cryptography;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            dd();
        }
        static async void dd()
        {
            Console.WriteLine("HERE");
            var encryptor = new Encryptor();
            var e = encryptor.Encrypt("Ayyy");
            var d = encryptor.Decrypt(e.Result);
            Console.WriteLine($"Key: {encryptor.Key} IV: {encryptor.IV}");
            Console.WriteLine(d.Result);
        }
    }
}
