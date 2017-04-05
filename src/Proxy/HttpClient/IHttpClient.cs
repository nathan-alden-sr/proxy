using System;
using System.Collections.Generic;
using System.Net;
using NathanAlden.Proxy.Http;

namespace NathanAlden.Proxy.HttpClient
{
    public interface IHttpClient
    {
        IPEndPoint Endpoint { get; }

        (GetRequestLineResult result, RequestLine requestLine) GetRequestLine();
        (GetResponseStatusLineResult result, ResponseStatusLine responseStatusLine) GetResponseStatusLine();
        (GetHeadersResult result, IEnumerable<Header> headers) GetHeaders();
        void WriteRequestLine(RequestLine requestLine);
        void WriteResponseStatusLine(ResponseStatusLine responseStatusLine);
        void WriteHeader(string name, string value);
        void WriteHeaders(IEnumerable<Header> headers);
        void WriteHeaders(params Header[] headers);
        void WriteNewLine();
        ArraySegment<byte> Read();
        void Write(ArraySegment<byte> arraySegment);
        void Flush();
        void Close();
    }
}