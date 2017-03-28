using System;
using System.Diagnostics.CodeAnalysis;

namespace NathanAlden.Proxy.Hosts
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum HostFormat
    {
        DomainName,
        DomainNameSuffix,
        IPv4,
        IPv4Cidr,
        IPv6,
        IPv6Cidr,
        All = DomainName | DomainNameSuffix | IPv4 | IPv4Cidr | IPv6 | IPv6Cidr
    }
}