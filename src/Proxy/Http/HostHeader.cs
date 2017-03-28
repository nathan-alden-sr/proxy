using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NathanAlden.Proxy.Http
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class HostHeader
    {
        private static readonly char[] _colon = { ':' };
        private static readonly Regex _regex = new Regex(@"^.+(:\d{1,5})?$");

        private HostHeader(string host, int? port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; }
        public int? Port { get; }
        private string DebuggerDisplay => ToString();

        public static HostHeader Parse(Header header)
        {
            if (header == null || !_regex.IsMatch(header.Value))
            {
                return null;
            }

            string[] valueParts = header.Value.Split(_colon);

            return new HostHeader(valueParts[0], valueParts.Length == 2 ? int.Parse(valueParts[1]) : (int?)null);
        }

        public override string ToString()
        {
            return $"Host: {Host}{(Port != null ? $":{Port}" : "")}";
        }
    }
}