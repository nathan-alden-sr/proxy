using System;
using System.Diagnostics;

namespace NathanAlden.Proxy.Http
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class Header
    {
        private static readonly char[] _colon = { ':' };

        public Header(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public string Value { get; }
        private string DebuggerDisplay => ToString();

        public static Header Parse(string header)
        {
            string[] headerParts = header.Split(_colon, 2, StringSplitOptions.RemoveEmptyEntries);
            string value = headerParts.Length == 2 ? headerParts[1].Trim(' ') : null;

            return !string.IsNullOrEmpty(value) ? new Header(headerParts[0], value) : null;
        }

        public override string ToString()
        {
            return $"{Name}: {Value}";
        }
    }
}