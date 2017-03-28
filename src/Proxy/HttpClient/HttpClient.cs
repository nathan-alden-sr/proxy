using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NathanAlden.Proxy.Http;
using NathanAlden.Proxy.Services.ConfigService;
using NathanAlden.Proxy.Tcp;

namespace NathanAlden.Proxy.HttpClient
{
    public class HttpClient : IHttpClient
    {
        private const byte CarriageReturn = (byte)'\r';
        private const byte LineFeed = (byte)'\n';
        private const string NewLine = "\r\n";
        private readonly TcpClientWrapper _clientWrapper;
        private readonly List<Header> _headers = new List<Header>();
        private byte[] _buffer = new byte[0];
        private bool _headersRead;
        private RequestLine _requestLine;
        private ResponseStatusLine _responseStatusLine;

        public HttpClient(TcpClient client, ConfigModel config)
        {
            _clientWrapper = new TcpClientWrapper(client, config);
            Endpoint = (IPEndPoint)client.Client.RemoteEndPoint;
        }

        public IPEndPoint Endpoint { get; }

        public (GetRequestLineResult result, RequestLine requestLine) GetRequestLine()
        {
            if (_requestLine != null)
            {
                return (GetRequestLineResult.Success, _requestLine);
            }

            string line = ReadLine();

            _requestLine = RequestLine.Parse(line);

            return _requestLine != null ? (GetRequestLineResult.Success, _requestLine) : (GetRequestLineResult.InvalidRequestLine, (RequestLine)null);
        }

        public (GetResponseStatusLineResult result, ResponseStatusLine responseStatusLine) GetResponseStatusLine()
        {
            if (_responseStatusLine != null)
            {
                return (GetResponseStatusLineResult.Success, _responseStatusLine);
            }

            string line = ReadLine();

            _responseStatusLine = ResponseStatusLine.Parse(line);

            return _responseStatusLine != null ? (GetResponseStatusLineResult.Success, _responseStatusLine) : (GetResponseStatusLineResult.InvalidResponseStatusLine, (ResponseStatusLine)null);
        }

        public (ReadHeaderResult result, Header header) ReadHeader()
        {
            if (_headersRead)
            {
                return (ReadHeaderResult.NoHeadersRemaining, null);
            }

            string line = ReadLine();

            if (line == "")
            {
                _headersRead = true;

                return (ReadHeaderResult.NoHeadersRemaining, null);
            }

            Header header = Header.Parse(line);

            if (header == null)
            {
                return (ReadHeaderResult.InvalidHeader, null);
            }

            _headers.Add(header);

            return (ReadHeaderResult.Success, header);
        }

        public (GetHeadersResult result, IEnumerable<Header> headers) GetHeaders()
        {
            while (!_headersRead)
            {
                (ReadHeaderResult readHeaderResult, Header _) = ReadHeader();

                switch (readHeaderResult)
                {
                    case ReadHeaderResult.Success:
                    case ReadHeaderResult.NoHeadersRemaining:
                        break;
                    case ReadHeaderResult.InvalidHeader:
                        return (GetHeadersResult.InvalidHeader, null);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (GetHeadersResult.Success, _headers.ToImmutableArray());
        }

        public void WriteRequestLine(RequestLine requestLine)
        {
            WriteLine(requestLine.ToString());
        }

        public void WriteResponeStatusLine(ResponseStatusLine responseStatusLine)
        {
            WriteLine(responseStatusLine.ToString());
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
            WriteLine("");
        }

        public byte[] ReadFromStream()
        {
            return _buffer.Length > 0 ? FlushBuffer() : _clientWrapper.ReadBytes();
        }

        public void WriteToStream(byte[] buffer)
        {
            _clientWrapper.WriteBytes(buffer);
        }

        public void FlushStream()
        {
            _clientWrapper.Flush();
        }

        public void Close()
        {
            _clientWrapper.Close();
        }

        private string ReadLine()
        {
            while (true)
            {
                int carriageReturnIndex = Array.IndexOf(_buffer, CarriageReturn);

                if (carriageReturnIndex != -1 && carriageReturnIndex < _buffer.Length - 1 && _buffer[carriageReturnIndex + 1] == LineFeed)
                {
                    string value = Encoding.ASCII.GetString(_buffer, 0, carriageReturnIndex);
                    int copyIndex = carriageReturnIndex + 2;
                    int newSize = _buffer.Length - copyIndex;

                    Array.Copy(_buffer, copyIndex, _buffer, 0, newSize);
                    Array.Resize(ref _buffer, newSize);

                    return value;
                }
                else
                {
                    byte[] buffer = ReadFromStream();
                    int copyIndex = _buffer.Length;

                    Array.Resize(ref _buffer, copyIndex + buffer.Length);
                    Array.Copy(buffer, 0, _buffer, copyIndex, buffer.Length);
                }
            }
        }

        private void WriteLine(string line)
        {
            byte[] buffer = Encoding.ASCII.GetBytes($"{line}{NewLine}");

            WriteToStream(buffer);
        }

        private byte[] FlushBuffer()
        {
            var buffer = (byte[])_buffer.Clone();

            Array.Resize(ref _buffer, 0);

            return buffer;
        }
    }
}