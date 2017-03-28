using System;
using System.Threading;
using Autofac;
using NathanAlden.Proxy.Ioc;
using NathanAlden.Proxy.Services.ConfigService;
using NathanAlden.Proxy.Services.CredentialService;
using NathanAlden.Proxy.Services.ListenerService;
using Serilog;

namespace NathanAlden.Proxy
{
    internal static class Program
    {
        private static IContainer _container;
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public static int Main()
        {
            Console.WriteLine("NTLM Proxy");
            Console.WriteLine("Written by Nathan Alden, Sr.");
            Console.WriteLine("https://github.com/nathan-alden/ntlm-proxy");
            Console.WriteLine();
            Console.WriteLine("Press CTRL+C to exit");
            Console.WriteLine();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Verbose()
                .CreateLogger();

            try
            {
                InitializeAutofac();
                InitializeConfig();

                var configService = _container.Resolve<IConfigService>();

                ConfigModel.ForwardProxiesModel.ForwardProxyModel httpForwardProxy = configService.Config.ForwardProxies?.Http;
                ConfigModel.ForwardProxiesModel.ForwardProxyModel httpsForwardProxy = configService.Config.ForwardProxies?.Https;

                if (httpForwardProxy?.Authentication.Basic.Enabled == true || httpsForwardProxy?.Authentication.Basic.Enabled == true)
                {
                    Console.WriteLine();
                    Console.WriteLine("A forward proxy requires basic authentication configuration.");
                    Console.WriteLine();

                    string username = httpForwardProxy?.Authentication.Basic.Username ?? httpsForwardProxy?.Authentication.Basic.Username;
                    var credentialService = _container.Resolve<ICredentialService>();

                    (GetCredentialsResult result, string username, string clearTextPassword) getCredentialsResult = credentialService.GetCredentials(username);

                    switch (getCredentialsResult.result)
                    {
                        case GetCredentialsResult.Success:
                            Console.WriteLine();
                            break;
                        case GetCredentialsResult.Canceled:
                            Log.Error("Forward proxy basic authentication is enabled but no credentials were supplied");

                            return ExitCodes.CredentialsNotSupplied;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                var listenerService = _container.Resolve<IListenerService>();

                Console.CancelKeyPress +=
                    (sender, args) =>
                    {
                        listenerService.Stop();
                        _cancellationTokenSource.Cancel();
                    };

                listenerService.Start();

                _cancellationTokenSource.Token.WaitHandle.WaitOne();

                return ExitCodes.Success;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Unhandled exception");

                return ExitCodes.UnhandledException;
            }
        }

        private static void InitializeAutofac()
        {
            var containerBuilder = new ContainerBuilder();

            AutofacRegistrar.RegisterComponents(containerBuilder);

            _container = containerBuilder.Build();
        }

        private static void InitializeConfig()
        {
            // ReSharper disable once UnusedVariable
            ConfigModel config = _container.Resolve<IConfigService>().Config;
        }

        public static void Run()
        {
        }
    }
}