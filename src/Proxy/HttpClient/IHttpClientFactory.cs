using System.Net.Sockets;

namespace NathanAlden.Proxy.HttpClient
{
    public interface IHttpClientFactory
    {
        IHttpClient Create(TcpClient client);
    }
}