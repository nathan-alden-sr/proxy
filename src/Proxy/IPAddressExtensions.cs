using System.Net;
using System.Net.Sockets;

namespace NathanAlden.Proxy
{
    public static class IPAddressExtensions
    {
        public static string ToBracketedString(this IPAddress ipAddress)
        {
            return ipAddress.AddressFamily == AddressFamily.InterNetwork ? ipAddress.ToString() : $"[{ipAddress}]";
        }
    }
}