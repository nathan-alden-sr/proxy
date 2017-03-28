using System;
using System.Diagnostics;

namespace NathanAlden.Proxy.Http
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class RequestLine
    {
        private static readonly char[] _space = { ' ' };

        private RequestLine(string method, string requestUri, Uri parsedRequestUri, string httpVersion)
        {
            Method = method;
            RequestUri = requestUri;
            ParsedRequestUri = parsedRequestUri;
            HttpVersion = httpVersion;
        }

        public string Method { get; }
        public string RequestUri { get; }
        public Uri ParsedRequestUri { get; }
        public string HttpVersion { get; }
        private string DebuggerDisplay => ToString();

        public override string ToString()
        {
            return $"{Method} {RequestUri} {HttpVersion}";
        }

        public static RequestLine Parse(string requestLine)
        {
            string[] requestLineParts = requestLine?.Split(_space, 3, StringSplitOptions.RemoveEmptyEntries);

            if (requestLineParts?.Length != 3)
            {
                return null;
            }

            return Uri.TryCreate(requestLineParts[1], UriKind.RelativeOrAbsolute, out Uri requestUri) ? new RequestLine(requestLineParts[0], requestLineParts[1], requestUri, requestLineParts[2]) : null;
        }
    }
}