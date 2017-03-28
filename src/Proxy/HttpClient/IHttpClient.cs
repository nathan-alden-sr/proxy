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
        (ReadHeaderResult result, Header header) ReadHeader();
        (GetHeadersResult result, IEnumerable<Header> headers) GetHeaders();
        void WriteRequestLine(RequestLine requestLine);
        void WriteResponeStatusLine(ResponseStatusLine responseStatusLine);
        void WriteHeaders(IEnumerable<Header> headers);
        void WriteHeaders(params Header[] headers);
        void WriteNewLine();
        byte[] ReadFromStream();
        void WriteToStream(byte[] buffer);
        void FlushStream();
        void Close();
    }
}