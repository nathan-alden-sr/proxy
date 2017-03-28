using System.Net.Sockets;

namespace NathanAlden.Proxy.Services.DownstreamClientService
{
    public interface IDownstreamClientService
    {
        int Add(TcpClient client);
        void CloseAll();
    }
}