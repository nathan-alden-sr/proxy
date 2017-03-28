using System;
using System.Collections.Generic;
using System.Net.Sockets;
using NathanAlden.Proxy.HttpClient;
using NathanAlden.Proxy.Services.ConfigService;
using NathanAlden.Proxy.Services.CredentialService;

namespace NathanAlden.Proxy.Services.DownstreamClientService
{
    public class DownstreamClientService : IDownstreamClientService
    {
        private readonly IConfigService _configService;
        private readonly ICredentialService _credentialService;
        private readonly List<DownstreamClient> _downstreamClients = new List<DownstreamClient>();
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly object _lockObject = new object();

        public DownstreamClientService(IHttpClientFactory httpClientFactory, ICredentialService credentialService, IConfigService configService)
        {
            _httpClientFactory = httpClientFactory;
            _credentialService = credentialService;
            _configService = configService;
        }

        public int Add(TcpClient client)
        {
            var downstreamClient = new DownstreamClient(_httpClientFactory, _credentialService, _configService, client);

            lock (_lockObject)
            {
                _downstreamClients.Add(downstreamClient);
            }

            downstreamClient.ClosedByClient.Subscribe(
                x =>
                {
                    lock (_lockObject)
                    {
                        _downstreamClients.Remove(downstreamClient);
                    }
                });

            downstreamClient.Run();

            return downstreamClient.Id;
        }

        public void CloseAll()
        {
            lock (_lockObject)
            {
                foreach (DownstreamClient downstreamClient in _downstreamClients)
                {
                    downstreamClient.Close();
                }
                _downstreamClients.Clear();
            }
        }
    }
}