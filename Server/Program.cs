using Networking;
using System.Security.Cryptography;
using System.Text;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var e = Encryptor.Encrypt("#5d'Url*Vl5qb*+,s%6p*2cb%&7Qcq+-&tyXveCgFeG#RG9gyaO,N.;k3bb+z9'EG/P-Uw0oM0/&vEZH,0sc(>I7yo:tcX-\"CcG87m1bPzJ3?u4yR8>PQ%2HPXYFCGm4", "Ayyyyy");
        }
    }
}
