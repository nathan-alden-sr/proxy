using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NathanAlden.Proxy.Services.ConfigService;

namespace NathanAlden.Proxy.Tcp
{
    public class TcpClientWrapper
    {
        private const int StreamBufferSize = 8192;
        private readonly TcpClient _client;
        private readonly BufferedStream _stream;

        public TcpClientWrapper(TcpClient client, ConfigModel config)
        {
            _client = client;

            client.ReceiveTimeout = config.Sockets.ReceiveTimeout;
            client.SendTimeout = config.Sockets.SendTimeout;

            _stream = new BufferedStream(client.GetStream(), StreamBufferSize);
            Endpoint = (IPEndPoint)client.Client.RemoteEndPoint;
        }

        public IPEndPoint Endpoint { get; }

        public byte[] ReadBytes()
        {
            var buffer = new byte[StreamBufferSize];
            int bytesRead = _stream.Read(buffer, 0, buffer.Length);

            Array.Resize(ref buffer, bytesRead);

            return buffer;
        }

        public void WriteBytes(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public void Close()
        {
            _client.Dispose();
        }
    }
}