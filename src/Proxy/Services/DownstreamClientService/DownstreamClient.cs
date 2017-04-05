using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NathanAlden.Proxy.Hosts;
using NathanAlden.Proxy.Http;
using NathanAlden.Proxy.HttpClient;
using NathanAlden.Proxy.Logging;
using NathanAlden.Proxy.Services.ConfigService;
using NathanAlden.Proxy.Services.CredentialService;
using Serilog.Events;

namespace NathanAlden.Proxy.Services.DownstreamClientService
{
    public class DownstreamClient
    {
        private static volatile int _nextId = 1;
        private static readonly IEnumerable<string> _validAbsoluteUriSchemes = new[] { "http", "https" };
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Subject<Unit> _closedByClient = new Subject<Unit>();
        private readonly IConfigService _configService;
        private readonly ICredentialService _credentialService;
        private readonly IEnumerable<Host> _disallowedHosts;
        private readonly IHttpClient _downstreamClient;
        private readonly ConfigModel.ForwardProxiesModel.ForwardProxyModel _forwardProxyHttp;
        private readonly ConfigModel.ForwardProxiesModel.ForwardProxyModel _forwardProxyHttps;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IEnumerable<Host> _noProxyHosts;
        private IHttpClient _upstreamServer;

        public DownstreamClient(IHttpClientFactory httpClientFactory, ICredentialService credentialService, IConfigService configService, TcpClient client)
        {
            _httpClientFactory = httpClientFactory;
            _credentialService = credentialService;
            _configService = configService;

            _downstreamClient = httpClientFactory.Create(client);
            _disallowedHosts = configService.Config.ParsedDisallowedHosts.ToImmutableArray();
            _forwardProxyHttp = configService.Config.ForwardProxies?.Http;
            _forwardProxyHttps = configService.Config.ForwardProxies?.Https;
            _noProxyHosts = (configService.Config.ForwardProxies?.ParsedNoProxyHosts ?? Enumerable.Empty<Host>()).ToImmutableArray();
            Id = _nextId++;
        }

        public int Id { get; }
        public IObservable<Unit> ClosedByClient => _closedByClient.AsObservable();

        public void Run()
        {
            Task.Run(async () =>
                     {
                         LogMessage LogDownstreamText(LogEventLevel level, string text = null) => LogMessage.Downstream(level, Id, _downstreamClient.Endpoint.Address, text);
                         LogMessage LogDownstreamException(Exception exception) => LogMessage.Downstream(exception, Id, _downstreamClient.Endpoint.Address);
                         LogMessage LogUpstreamText(LogEventLevel level, string text = null) => LogMessage.Upstream(level, Id, _upstreamServer.Endpoint.Address, text);
                         LogMessage LogUpstreamException(Exception exception) => LogMessage.Upstream(exception, Id, _upstreamServer.Endpoint.Address);
                         LogMessage LogForwardProxyText(LogEventLevel level, string text = null) => LogMessage.ForwardProxy(level, Id, _upstreamServer.Endpoint.Address, text);
                         LogMessage LogForwardProxyException(Exception exception) => LogMessage.ForwardProxy(exception, Id, _upstreamServer.Endpoint.Address);

                         RequestLine requestLine;
                         IEnumerable<Header> headers;
                         HostHeader hostHeader;

                         try
                         {
                             (GetRequestLineResult result, RequestLine requestLine) getRequestLineResult = _downstreamClient.GetRequestLine();

                             switch (getRequestLineResult.result)
                             {
                                 case GetRequestLineResult.Success:
                                     requestLine = getRequestLineResult.requestLine;
                                     break;
                                 case GetRequestLineResult.InvalidRequestLine:
                                     LogDownstreamText(LogEventLevel.Error, "Invalid request line").Write();
                                     goto close;
                                 default:
                                     throw new ArgumentOutOfRangeException();
                             }

                             (GetHeadersResult result, IEnumerable<Header> headers) getHeadersResult = _downstreamClient.GetHeaders();

                             getHeadersResult.headers = getHeadersResult.headers ?? Enumerable.Empty<Header>();

                             switch (getHeadersResult.result)
                             {
                                 case GetHeadersResult.Success:
                                     headers = getHeadersResult.headers.ToImmutableArray();
                                     break;
                                 case GetHeadersResult.InvalidHeader:
                                     LogDownstreamText(LogEventLevel.Error, "Invalid header").Write();
                                     goto close;
                                 default:
                                     throw new ArgumentOutOfRangeException();
                             }

                             hostHeader = HostHeader.Parse(getHeadersResult.headers.FirstOrDefault(x => x.Name.Equals("Host", StringComparison.OrdinalIgnoreCase)));

                             LogDownstreamText(LogEventLevel.Information).RightArrow(true).Text($"{getRequestLineResult.requestLine}{(hostHeader != null ? $", {hostHeader}" : "")}").Write();
                         }
                         catch (Exception exception)
                         {
                             LogDownstreamException(exception).Write();
                             goto close;
                         }

                         bool isTunnel = requestLine.Method == "CONNECT";
                         string upstreamHost;
                         int upstreamPort;

                         if (requestLine.ParsedRequestUri.IsAbsoluteUri && _validAbsoluteUriSchemes.Contains(requestLine.ParsedRequestUri.Scheme, StringComparer.OrdinalIgnoreCase))
                         {
                             upstreamHost = requestLine.ParsedRequestUri.Host;
                             upstreamPort = requestLine.ParsedRequestUri.Port;
                         }
                         else if (hostHeader != null)
                         {
                             upstreamHost = hostHeader.Host;
                             upstreamPort = hostHeader.Port ?? (isTunnel ? 443 : 80);
                         }
                         else
                         {
                             LogDownstreamText(LogEventLevel.Error, "Missing Host header").Write();
                             goto close;
                         }

                         if (_disallowedHosts.Any(x => x.Contains(upstreamHost) == ContainsResult.Yes))
                         {
                             LogDownstreamText(LogEventLevel.Error, $"Host '{upstreamHost}' is disallowed").Write();
                             goto close;
                         }

                         ConfigModel.ForwardProxiesModel.ForwardProxyModel forwardProxy = isTunnel ? _forwardProxyHttps : _forwardProxyHttp;

                         async Task<bool> ConnectToUpstreamServerAsync()
                         {
                             _upstreamServer?.Close();

                             var upstreamServer = new TcpClient();

                             try
                             {
                                 if (forwardProxy != null && _noProxyHosts.All(x => x.Contains(upstreamHost) != ContainsResult.Yes))
                                 {
                                     await upstreamServer.ConnectAsync(forwardProxy.Host, forwardProxy.Port);
                                 }
                                 else
                                 {
                                     forwardProxy = null;
                                     await upstreamServer.ConnectAsync(upstreamHost, upstreamPort);
                                 }
                             }
                             catch (Exception exception)
                             {
                                 LogDownstreamException(exception).Write();
                                 return false;
                             }

                             _upstreamServer = _httpClientFactory.Create(upstreamServer);

                             return true;
                         }

                         if (!await ConnectToUpstreamServerAsync())
                         {
                             goto close;
                         }

                         if (isTunnel)
                         {
                             var responseStatusLine = new ResponseStatusLine(requestLine.HttpVersion, 200);

                             _downstreamClient.WriteResponseStatusLine(responseStatusLine);
                             if (_configService.Config.Options.SendProxyAgentHeader)
                             {
                                 _downstreamClient.WriteHeader("Proxy-Agent", typeof(Program).Namespace);
                             }
                             _downstreamClient.WriteNewLine();
                             _downstreamClient.Flush();

                             LogDownstreamText(LogEventLevel.Information).LeftArrow(true).Text(responseStatusLine.ToString()).Write();

                             if (forwardProxy != null)
                             {
                                 LogForwardProxyText(LogEventLevel.Information).LeftArrow(true).Text($"{requestLine}{(hostHeader != null ? $", {hostHeader}" : "")}").Write();

                                 _upstreamServer.WriteRequestLine(requestLine);
                                 _upstreamServer.WriteHeaders(hostHeader);
                                 _upstreamServer.WriteNewLine();

                                 (GetResponseStatusLineResult result, ResponseStatusLine responseStatusLine) responseStatusLineResult = _upstreamServer.GetResponseStatusLine();

                                 switch (responseStatusLineResult.result)
                                 {
                                     case GetResponseStatusLineResult.Success:
                                         LogForwardProxyText(LogEventLevel.Information).RightArrow(true).Text(responseStatusLineResult.responseStatusLine.ToString()).Write();
                                         break;
                                     case GetResponseStatusLineResult.InvalidResponseStatusLine:
                                         LogForwardProxyText(LogEventLevel.Error, "Invalid response status line").Write();
                                         break;
                                     default:
                                         throw new ArgumentOutOfRangeException();
                                 }

                                 _upstreamServer.GetHeaders();
                             }
                         }
                         else
                         {
                             void WriteUpstreamServerRequest(params Header[] additionalHeaders)
                             {
                                 _upstreamServer.WriteRequestLine(requestLine);
                                 _upstreamServer.WriteHeaders(headers);
                                 if (_configService.Config.Options.SendProxyAgentHeader)
                                 {
                                     _upstreamServer.WriteHeaders(Header.Parse($"Proxy-Agent: {typeof(Program).Namespace}"));
                                 }
                                 _upstreamServer.WriteHeaders(additionalHeaders);
                                 _upstreamServer.WriteNewLine();
                                 _upstreamServer.Flush();
                             }

                             WriteUpstreamServerRequest();

                             if (forwardProxy != null && forwardProxy.Authentication.Basic.Enabled)
                             {
                                 (GetResponseStatusLineResult result, ResponseStatusLine responseStatusLine) getResponseStatusLineResult = _upstreamServer.GetResponseStatusLine();

                                 switch (getResponseStatusLineResult.result)
                                 {
                                     case GetResponseStatusLineResult.Success:
                                         break;
                                     case GetResponseStatusLineResult.InvalidResponseStatusLine:
                                         LogUpstreamText(LogEventLevel.Error, "Invalid response line");
                                         goto close;
                                     default:
                                         throw new ArgumentOutOfRangeException();
                                 }

                                 if (getResponseStatusLineResult.responseStatusLine.StatusCode == 407)
                                 {
                                     (GetHeadersResult result, IEnumerable<Header> headers) getHeadersResult = _upstreamServer.GetHeaders();

                                     switch (getHeadersResult.result)
                                     {
                                         case GetHeadersResult.Success:
                                             break;
                                         case GetHeadersResult.InvalidHeader:
                                             LogUpstreamText(LogEventLevel.Error, "Invalid header").Write();
                                             goto close;
                                         default:
                                             throw new ArgumentOutOfRangeException();
                                     }

                                     LogForwardProxyText(LogEventLevel.Warning).RightArrow(true).Text(getResponseStatusLineResult.responseStatusLine.ToString()).Write();

                                     if (!await ConnectToUpstreamServerAsync())
                                     {
                                         goto close;
                                     }

                                     (GetCredentialsResult result, string username, string clearTextPassword) getCredentialsResult = _credentialService.GetCredentials();
                                     string base64Credential = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{getCredentialsResult.username}:{getCredentialsResult.clearTextPassword}"));

                                     LogForwardProxyText(LogEventLevel.Warning).LeftArrow(true).Text("Proxy authorization").Write();

                                     WriteUpstreamServerRequest(new Header("Proxy-Authorization", $"Basic {base64Credential}"));
                                 }
                                 else
                                 {
                                     _downstreamClient.WriteResponseStatusLine(getResponseStatusLineResult.responseStatusLine);
                                 }
                             }
                         }

                         var manualResetEvent = new ManualResetEventSlim();

#pragma warning disable 4014
                         Task.Run(() =>
#pragma warning restore 4014
                                  {
                                      try
                                      {
                                          while (true)
                                          {
                                              ArraySegment<byte> arraySegment = _downstreamClient.Read();

                                              if (arraySegment.Count == 0)
                                              {
                                                  break;
                                              }

                                              _upstreamServer.Write(arraySegment);
                                              _upstreamServer.Flush();

                                              LogDownstreamText(LogEventLevel.Verbose)
                                                  .RightArrow(true)
                                                  .Text($"{arraySegment.Count} byte{(arraySegment.Count == 1 ? "" : "s")}")
                                                  .RightArrow(true)
                                                  .UpstreamIdentifier(Id)
                                                  .Space()
                                                  .BracketedIpAddress(_upstreamServer.Endpoint.Address)
                                                  .Write();
                                          }
                                      }
                                      catch (Exception exception)
                                      {
                                          LogDownstreamException(exception).Write();
                                      }
                                      finally
                                      {
                                          manualResetEvent.Set();
                                      }
                                  });

#pragma warning disable 4014
                         Task.Run(() =>
#pragma warning restore 4014
                                  {
                                      try
                                      {
                                          while (true)
                                          {
                                              ArraySegment<byte> arraySegment = _upstreamServer.Read();

                                              if (arraySegment.Count == 0)
                                              {
                                                  break;
                                              }

                                              _downstreamClient.Write(arraySegment);
                                              _downstreamClient.Flush();

                                              (forwardProxy != null ? LogForwardProxyText(LogEventLevel.Verbose) : LogUpstreamText(LogEventLevel.Verbose))
                                                  .RightArrow(true)
                                                  .Text($"{arraySegment.Count} byte{(arraySegment.Count == 1 ? "" : "s")}")
                                                  .RightArrow(true)
                                                  .DownstreamIdentifier(Id)
                                                  .Space()
                                                  .BracketedIpAddress(_downstreamClient.Endpoint.Address)
                                                  .Write();
                                          }
                                      }
                                      catch (Exception exception)
                                      {
                                          (forwardProxy != null ? LogForwardProxyException(exception) : LogUpstreamException(exception)).Write();
                                      }
                                      finally
                                      {
                                          manualResetEvent.Set();
                                      }
                                  });

                         manualResetEvent.Wait();

                         close:
                         CloseConnections();

                         LogDownstreamText(LogEventLevel.Information, "Connection closed").Write();

                         _closedByClient.OnNext(Unit.Default);
                     });
        }

        public void Close()
        {
            CloseConnections();
            _cancellationTokenSource.Cancel();
        }

        private void CloseConnections()
        {
            _upstreamServer?.Close();
            _downstreamClient.Close();
        }
    }
}