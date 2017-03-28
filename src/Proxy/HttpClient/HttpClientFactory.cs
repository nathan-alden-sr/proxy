using System.Net.Sockets;
using NathanAlden.Proxy.Services.ConfigService;

namespace NathanAlden.Proxy.HttpClient
{
    public class HttpClientFactory : IHttpClientFactory
    {
        private readonly IConfigService _configService;

        public HttpClientFactory(IConfigService configService)
        {
            _configService = configService;
        }

        public IHttpClient Create(TcpClient client)
        {
            return new HttpClient(client, _configService.Config);
        }
    }
}