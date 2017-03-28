using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NathanAlden.Proxy.Logging;
using NathanAlden.Proxy.Services.ConfigService;
using NathanAlden.Proxy.Services.DownstreamClientService;
using Serilog;
using Serilog.Events;

namespace NathanAlden.Proxy.Services.ListenerService
{
    public class ListenerService : IListenerService
    {
        private readonly IConfigService _configService;
        private readonly IDownstreamClientService _downstreamClientService;
        private readonly object _lockObject = new object();
        private ImmutableList<TcpListener> _listeners;

        public ListenerService(IDownstreamClientService downstreamClientService, IConfigService configService)
        {
            _downstreamClientService = downstreamClientService;
            _configService = configService;
        }

        public void Start()
        {
            lock (_lockObject)
            {
                _listeners = _configService.Config.Bindings.IpAddresses.Select(x => CreateListener(x, _configService.Config.Bindings.Port)).Where(x => x != null).ToImmutableList();

                _listeners.ForEach(
                    x =>
                    {
                        Task.Run(async () =>
                                 {
                                     while (true)
                                     {
                                         TcpClient client;

                                         try
                                         {
                                             client = await x.AcceptTcpClientAsync();
                                         }
                                         catch
                                         {
                                             return;
                                         }

                                         var endpoint = (IPEndPoint)client.Client.RemoteEndPoint;
                                         int id = _downstreamClientService.Add(client);

                                         LogMessage.Downstream(LogEventLevel.Information, id, endpoint.Address, "Connection accepted").Write();
                                     }
                                 });
                    });
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _listeners.ForEach(
                    x =>
                    {
                        x.Stop();

                        var endpoint = (IPEndPoint)x.LocalEndpoint;

                        Log.Information($"No longer listening on {endpoint.Address.ToBracketedString()}:{endpoint.Port}");
                    });

                _downstreamClientService.CloseAll();
            }
        }

        private static TcpListener CreateListener(string ipAddress, int port)
        {
            IPAddress parsedIpAddress = IPAddress.Parse(ipAddress);
            var listener = new TcpListener(parsedIpAddress, port);

            try
            {
                listener.Start();
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"Error listening on {parsedIpAddress.ToBracketedString()}:{port}");
                return null;
            }

            Log.Information($"Listening on {parsedIpAddress.ToBracketedString()}:{port}");

            return listener;
        }
    }
}