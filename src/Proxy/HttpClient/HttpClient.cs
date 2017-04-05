using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NathanAlden.Proxy.Http;
using NathanAlden.Proxy.Tcp;

namespace NathanAlden.Proxy.HttpClient
{
    public class HttpClient : IHttpClient
    {
        private readonly ArraySegmentsToHttpHeaderLines _arraySegmentsToHttpHeaderLines = new ArraySegmentsToHttpHeaderLines();
        private readonly TcpClientWrapper _clientWrapper;
        private readonly Queue<(HttpHeaderLineType lineType, string line)> _headerLines = new Queue<(HttpHeaderLineType lineType, string line)>();
        private readonly List<Header> _headers = new List<Header>();
        private bool _headersComplete;
        private RequestLine _requestLine;
        private ResponseStatusLine _responseStatusLine;

        public HttpClient(TcpClientWrapper clientWrapper)
        {
            _clientWrapper = clientWrapper;
        }

        public IPEndPoint Endpoint => _clientWrapper.Endpoint;

        public (GetRequestLineResult result, RequestLine requestLine) GetRequestLine()
        {
            while (_requestLine == null)
            {
                ReadHeaderLines(_headerLines);

                if (_headerLines.Count == 0)
                {
                    continue;
                }

                (HttpHeaderLineType lineType, string line) headerLine = _headerLines.Dequeue();

                if (headerLine.lineType != HttpHeaderLineType.RequestLineOrResponseStatusLine)
                {
                    return (GetRequestLineResult.InvalidRequestLine, null);
                }

                _requestLine = RequestLine.Parse(headerLine.line);
            }

            return (_requestLine == null ? GetRequestLineResult.InvalidRequestLine : GetRequestLineResult.Success, _requestLine);
        }

        public (GetResponseStatusLineResult result, ResponseStatusLine responseStatusLine) GetResponseStatusLine()
        {
            while (_responseStatusLine == null)
            {
                ReadHeaderLines(_headerLines);

                if (_headerLines.Count == 0)
                {
                    continue;
                }

                (HttpHeaderLineType lineType, string line) headerLine = _headerLines.Dequeue();

                if (headerLine.lineType != HttpHeaderLineType.RequestLineOrResponseStatusLine)
                {
                    return (GetResponseStatusLineResult.InvalidResponseStatusLine, null);
                }

                _responseStatusLine = ResponseStatusLine.Parse(headerLine.line);
            }

            return (_responseStatusLine == null ? GetResponseStatusLineResult.InvalidResponseStatusLine : GetResponseStatusLineResult.Success, _responseStatusLine);
        }

        public (GetHeadersResult result, IEnumerable<Header> headers) GetHeaders()
        {
            do
            {
                while (_headerLines.Count > 0)
                {
                    (HttpHeaderLineType lineType, string line) headerLine = _headerLines.Dequeue();

                    switch (headerLine.lineType)
                    {
                        case HttpHeaderLineType.Header:
                            Header header = Header.Parse(headerLine.line);

                            if (header == null)
                            {
                                return (GetHeadersResult.InvalidHeader, null);
                            }

                            _headers.Add(header);
                            break;
                        case HttpHeaderLineType.NewLine:
                            _headersComplete = true;
                            break;
                        case HttpHeaderLineType.RequestLineOrResponseStatusLine:
                            return (GetHeadersResult.InvalidHeader, null);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (!_headersComplete)
                {
                    ReadHeaderLines(_headerLines);
                }
            } while (!_headersComplete);

            return (GetHeadersResult.Success, _headers);
        }

        public void WriteRequestLine(RequestLine requestLine)
        {
            WriteLine(requestLine.ToString());
        }

        public void WriteResponseStatusLine(ResponseStatusLine responseStatusLine)
        {
            WriteLine(responseStatusLine.ToString());
        }

        public void WriteHeader(string name, string value)
        {
            WriteLine($"{name}: {value}");
        }

        public void WriteHeaders(IEnumerable<Header> headers)
        {
            foreach (Header header in headers)
            {
                WriteLine(header.ToString());
            }
        }

        public void WriteHeaders(params Header[] headers)
        {
            WriteHeaders((IEnumerable<Header>)headers);
        }

        public void WriteNewLine()
        {
            _clientWrapper.WriteByte(HttpConstants.CarriageReturn);
            _clientWrapper.WriteByte(HttpConstants.LineFeed);
        }

        public ArraySegment<byte> Read()
        {
            return _clientWrapper.Read();
        }

        public void Write(ArraySegment<byte> arraySegment)
        {
            _clientWrapper.Write(arraySegment);
        }

        public void Flush()
        {
            _clientWrapper.Flush();
        }

        public void Close()
        {
            _clientWrapper.Close();
        }

        private void ReadHeaderLines(Queue<(HttpHeaderLineType lineType, string line)> headerLines)
        {
            ArraySegment<byte> arraySegment = _clientWrapper.Read();

            PushHeaderLinesToStack(_arraySegmentsToHttpHeaderLines.Add(arraySegment), headerLines);
        }

        private void WriteLine(string line)
        {
            byte[] buffer = Encoding.ASCII.GetBytes($"{line}{HttpConstants.NewLine}");

            _clientWrapper.Write(new ArraySegment<byte>(buffer));
        }

        private static void PushHeaderLinesToStack(IEnumerable<(HttpHeaderLineType lineType, string line)> headerLines, Queue<(HttpHeaderLineType lineType, string line)> headerLineStack)
        {
            foreach ((HttpHeaderLineType lineType, string line) headerLine in headerLines)
            {
                headerLineStack.Enqueue(headerLine);
            }
        }
    }
}