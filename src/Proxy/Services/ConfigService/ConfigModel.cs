using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using NathanAlden.Proxy.Hosts;
using YamlDotNet.Serialization;

namespace NathanAlden.Proxy.Services.ConfigService
{
    public class ConfigModel
    {
        private BindingsModel _bindings = new BindingsModel();
        private IEnumerable<string> _disallowedHosts = Enumerable.Empty<string>();
        private ForwardProxiesModel _forwardProxies = new ForwardProxiesModel();
        private OptionsModel _options = new OptionsModel();
        private SocketsModel _sockets = new SocketsModel();

        public BindingsModel Bindings
        {
            get => _bindings;
            set => _bindings = value ?? new BindingsModel();
        }

        public IEnumerable<string> DisallowedHosts
        {
            get => _disallowedHosts;
            set => _disallowedHosts = value ?? Enumerable.Empty<string>();
        }

        public IEnumerable<Host> ParsedDisallowedHosts => DisallowedHosts.Select(x => Host.TryParse(x, out Host host) ? host : null).Where(x => x != null);

        public ForwardProxiesModel ForwardProxies
        {
            get => _forwardProxies;
            set => _forwardProxies = value ?? new ForwardProxiesModel();
        }

        public SocketsModel Sockets
        {
            get => _sockets;
            set => _sockets = value ?? new SocketsModel();
        }

        public OptionsModel Options
        {
            get => _options;
            set => _options = value ?? new OptionsModel();
        }

        public class BindingsModel
        {
            private IEnumerable<string> _ipAddresses = new[] { IPAddress.Loopback.ToString(), IPAddress.IPv6Loopback.ToString() };

            [Required]
            [MinLength(1)]
            [YamlMember(typeof(string))]
            public IEnumerable<string> IpAddresses
            {
                get => _ipAddresses;
                set => _ipAddresses = value ?? new[] { IPAddress.Loopback.ToString(), IPAddress.IPv6Loopback.ToString() };
            }

            public IEnumerable<Host> ParsedIpAddresses => IpAddresses.Select(x => Host.TryParse(x, HostFormat.IPv4 | HostFormat.IPv6, out Host host) ? host : null).Where(x => x != null);

            [Required]
            public int Port { get; set; } = 3128;
        }

        public class ForwardProxiesModel
        {
            private IEnumerable<string> _noProxyHosts = Enumerable.Empty<string>();
            public ForwardProxyModel Http { get; set; }
            public ForwardProxyModel Https { get; set; }

            public IEnumerable<string> NoProxyHosts
            {
                get => _noProxyHosts;
                set => _noProxyHosts = value ?? Enumerable.Empty<string>();
            }

            public IEnumerable<Host> ParsedNoProxyHosts => NoProxyHosts.Select(x => Host.TryParse(x, out Host host) ? host : null).Where(x => x != null);

            public class ForwardProxyModel
            {
                private AuthenticationModel _authentication = new AuthenticationModel();

                [Required]
                public string Host { get; set; }

                public Host ParsedHost => Hosts.Host.TryParse(Host, HostFormat.DomainName | HostFormat.IPv4 | HostFormat.IPv6, out Host host) ? host : throw new InvalidOperationException("Host is invalid.");

                [Required]
                public int Port { get; set; }

                public AuthenticationModel Authentication
                {
                    get => _authentication;
                    set => _authentication = value ?? new AuthenticationModel();
                }

                public class AuthenticationModel
                {
                    private BasicModel _basic = new BasicModel();

                    public BasicModel Basic
                    {
                        get => _basic;
                        set => _basic = value ?? new BasicModel();
                    }

                    public class BasicModel
                    {
                        public bool Enabled { get; set; }
                        public string Username { get; set; }
                    }
                }
            }
        }

        public class SocketsModel
        {
            public int ReceiveTimeout { get; set; } = 120000;
            public int SendTimeout { get; set; } = 120000;
        }

        public class OptionsModel
        {
            public bool SendProxyAgentHeader { get; set; }
        }
    }
}