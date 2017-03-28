using Autofac;
using NathanAlden.Proxy.HttpClient;
using NathanAlden.Proxy.Services.ConfigService;
using NathanAlden.Proxy.Services.CredentialService;
using NathanAlden.Proxy.Services.DownstreamClientService;
using NathanAlden.Proxy.Services.ListenerService;

namespace NathanAlden.Proxy.Ioc
{
    public static class AutofacRegistrar
    {
        public static void RegisterComponents(ContainerBuilder containerBuilder)
        {
            containerBuilder.RegisterType<ConfigService>().As<IConfigService>().SingleInstance();
            containerBuilder.RegisterType<CredentialService>().As<ICredentialService>().SingleInstance();
            containerBuilder.RegisterType<DownstreamClientService>().As<IDownstreamClientService>().SingleInstance();
            containerBuilder.RegisterType<ListenerService>().As<IListenerService>().SingleInstance();
            containerBuilder.RegisterType<HttpClientFactory>().As<IHttpClientFactory>().SingleInstance();
        }
    }
}