using System.Net.Sockets;
using NathanAlden.Proxy.Services.ConfigService;
using NathanAlden.Proxy.Tcp;

namespace NathanAlden.Proxy.HttpClient
{
    public class HttpClientFactory : IHttpClientFactory
    {
        private const int BufferSize = 8192;
        private readonly IConfigService _configService;

        public HttpClientFactory(IConfigService configService)
        {
            _configService = configService;
        }

        public IHttpClient Create(TcpClient client)
        {
            var clientWrapper = new TcpClientWrapper(client, BufferSize, _configService.Config.Sockets.ReceiveTimeout, BufferSize, _configService.Config.Sockets.SendTimeout, BufferSize);

            return new HttpClient(clientWrapper);
        }
    }
}