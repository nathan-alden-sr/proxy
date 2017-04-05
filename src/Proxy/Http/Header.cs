using System;
using System.Diagnostics;

namespace NathanAlden.Proxy.Http
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class Header
    {
        private static readonly char[] _colon = { ':' };
        private static readonly char[] _space = { ' ' };

        public Header(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }
        private string DebuggerDisplay => ToString();

        public override string ToString()
        {
            return $"{Name}: {Value}";
        }

        public static Header Parse(string header)
        {
            string[] headerParts = header.Split(_colon, 2, StringSplitOptions.RemoveEmptyEntries);

            return headerParts.Length == 2 ? new Header(headerParts[0], headerParts[1].Trim(_space)) : null;
        }
    }
}