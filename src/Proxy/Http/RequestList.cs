using System;
using System.Diagnostics;

namespace NathanAlden.Proxy.Http
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class RequestLine
    {
        private static readonly char[] _space = { ' ' };

        public RequestLine(string method, string requestUri, string httpVersion)
            : this(method, requestUri, ParseRequestUri(requestUri), httpVersion)
        {
        }

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

        private static Uri ParseRequestUri(string requestUri)
        {
            return Uri.TryCreate(requestUri, UriKind.RelativeOrAbsolute, out Uri parsedRequestUri) ? parsedRequestUri : null;
        }

        public static RequestLine Parse(string requestLine)
        {
            string[] requestLineParts = requestLine?.Split(_space, 3, StringSplitOptions.RemoveEmptyEntries);

            return requestLineParts?.Length == 3 ? new RequestLine(requestLineParts[0], requestLineParts[1], requestLineParts[2]) : null;
        }
    }
}