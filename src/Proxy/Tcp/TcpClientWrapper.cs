using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NathanAlden.Proxy.Tcp
{
    public class TcpClientWrapper
    {
        private readonly byte[] _buffer;
        private readonly TcpClient _client;
        private readonly BufferedStream _stream;

        public TcpClientWrapper(TcpClient client, int receiveBufferSize, int receiveTimeoutInMilliseconds, int sendBufferSize, int sendTimeoutInMilliseconds, int streamBufferSize)
        {
            _client = client;

            client.ReceiveBufferSize = receiveBufferSize;
            client.ReceiveTimeout = receiveTimeoutInMilliseconds;
            client.SendBufferSize = sendBufferSize;
            client.SendTimeout = sendTimeoutInMilliseconds;

            _stream = new BufferedStream(client.GetStream(), streamBufferSize);
            _buffer = new byte[streamBufferSize];
            Endpoint = (IPEndPoint)_client.Client.RemoteEndPoint;
        }

        public IPEndPoint Endpoint { get; }

        public ArraySegment<byte> Read()
        {
            int bytesRead = _stream.Read(_buffer, 0, _buffer.Length);

            return new ArraySegment<byte>(_buffer, 0, bytesRead);
        }

        public void Write(ArraySegment<byte> arraySegment)
        {
            _stream.Write(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        public void WriteByte(byte value)
        {
            _stream.WriteByte(value);
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