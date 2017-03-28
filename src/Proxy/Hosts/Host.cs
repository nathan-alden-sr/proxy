using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace NathanAlden.Proxy.Hosts
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class Host
    {
        private static readonly Regex _cidrRegex = new Regex(@"(?<IpAddress>.+)/(?<Cidr>\d+)$");

        private Host(HostFormat format, string domainName, IPAddress ipAddress = null, int? maskBits = null)
        {
            Format = format;
            DomainName = domainName;
            IpAddress = ipAddress;
            MaskBits = maskBits;
        }

        public HostFormat Format { get; }
        public string DomainName { get; }
        public IPAddress IpAddress { get; }
        public int? MaskBits { get; }

        private string DebuggerDisplay
        {
            // ReSharper disable once UnusedMember.Local
            get
            {
                switch (Format)
                {
                    case HostFormat.DomainName:
                        return $"Domain name: {ToString()}";
                    case HostFormat.DomainNameSuffix:
                        return $"Domain name suffix: {ToString()}";
                    case HostFormat.IPv4:
                        return $"IPv4: {ToString()}";
                    case HostFormat.IPv4Cidr:
                        return $"IPv4 CIDR: {ToString()}";
                    case HostFormat.IPv6:
                        return $"IPv6: {ToString()}";
                    case HostFormat.IPv6Cidr:
                        return $"IPv6 CIDR: {ToString()}";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public ContainsResult Contains(Host host)
        {
            if (host.Format != HostFormat.DomainName && host.Format != HostFormat.IPv4 && host.Format != HostFormat.IPv6)
            {
                throw new ArgumentException($"Format must be {HostFormat.DomainName}, {HostFormat.IPv4}, or {HostFormat.IPv6}.");
            }

            switch (host.Format)
            {
                case HostFormat.DomainName:
                    switch (Format)
                    {
                        case HostFormat.DomainName:
                            return DomainName.Equals(host.DomainName, StringComparison.OrdinalIgnoreCase) ? ContainsResult.Yes : ContainsResult.No;
                        case HostFormat.DomainNameSuffix:
                            return host.DomainName.EndsWith(DomainName, StringComparison.OrdinalIgnoreCase) ? ContainsResult.Yes : ContainsResult.No;
                    }
                    break;
                case HostFormat.IPv4:
                    switch (Format)
                    {
                        case HostFormat.IPv4:
                            return IpAddress.Equals(host.IpAddress) ? ContainsResult.Yes : ContainsResult.No;
                        case HostFormat.IPv4Cidr:
                            // ReSharper disable once PossibleInvalidOperationException
                            return CidrMatches(IpAddress, host.IpAddress, MaskBits.Value, 32) ? ContainsResult.Yes : ContainsResult.No;
                    }
                    break;
                case HostFormat.IPv6:
                    switch (Format)
                    {
                        case HostFormat.IPv6:
                            return IpAddress.Equals(host.IpAddress) ? ContainsResult.Yes : ContainsResult.No;
                        case HostFormat.IPv6Cidr:
                            // ReSharper disable once PossibleInvalidOperationException
                            return CidrMatches(IpAddress, host.IpAddress, MaskBits.Value, 128) ? ContainsResult.Yes : ContainsResult.No;
                    }
                    break;
            }

            return ContainsResult.Inapplicable;
        }

        public ContainsResult Contains(string host)
        {
            return TryParse(host, HostFormat.DomainName | HostFormat.IPv4 | HostFormat.IPv6, out Host parsedHost)
                ? Contains(parsedHost)
                : throw new ArgumentException($"Format must be {HostFormat.DomainName}, {HostFormat.IPv4}, or {HostFormat.IPv6}.");
        }

        public override string ToString()
        {
            switch (Format)
            {
                case HostFormat.DomainName:
                case HostFormat.DomainNameSuffix:
                    return DomainName;
                case HostFormat.IPv4:
                case HostFormat.IPv6:
                    return IpAddress.ToString();
                case HostFormat.IPv4Cidr:
                case HostFormat.IPv6Cidr:
                    return $"{IpAddress}/{MaskBits}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool TryParse(string value, HostFormat validFormats, out Host host)
        {
            host = null;

            HostFormat format;
            string domainName;
            string ipAddress;
            int? maskBits;
            Match match = _cidrRegex.Match(value);
            IPAddress parsedIpAddress;

            if (match.Success)
            {
                ipAddress = match.Groups["IpAddress"].Value;
                maskBits = int.Parse(match.Groups["Cidr"].Value);
            }
            else
            {
                ipAddress = value;
                maskBits = null;
            }
            if (IPAddress.TryParse(ipAddress, out parsedIpAddress))
            {
                switch (parsedIpAddress.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        if (maskBits == null && !validFormats.HasFlag(HostFormat.IPv4) || maskBits != null && (!validFormats.HasFlag(HostFormat.IPv4Cidr) || maskBits > 32))
                        {
                            return false;
                        }
                        format = maskBits != null ? HostFormat.IPv4Cidr : HostFormat.IPv4;
                        break;
                    case AddressFamily.InterNetworkV6:
                        if (maskBits == null && !validFormats.HasFlag(HostFormat.IPv6) || maskBits != null && (!validFormats.HasFlag(HostFormat.IPv6Cidr) || maskBits > 128))
                        {
                            return false;
                        }
                        format = maskBits != null ? HostFormat.IPv6Cidr : HostFormat.IPv6;
                        break;
                    default:
                        throw new ArgumentException($"Unexpected address family '{parsedIpAddress.AddressFamily}'.");
                }

                domainName = null;
            }
            else
            {
                format = value.StartsWith(".", StringComparison.OrdinalIgnoreCase) ? HostFormat.DomainNameSuffix : HostFormat.DomainName;

                if (!validFormats.HasFlag(format))
                {
                    return false;
                }

                domainName = value;
            }

            host = new Host(format, domainName, parsedIpAddress, maskBits);

            return true;
        }

        public static bool TryParse(string value, out Host host)
        {
            return TryParse(value, HostFormat.All, out host);
        }

        private static bool CidrMatches(IPAddress cidrIpAddress, IPAddress ipAddress, int maskBits, int totalBits)
        {
            var cidrIpAddressBitArray = new BitArray(cidrIpAddress.GetAddressBytes());
            var ipAddressBitArray = new BitArray(ipAddress.GetAddressBytes());
            var maskBitArray = new BitArray(totalBits);

            for (var i = 0; i < maskBits; i++)
            {
                maskBitArray.Set(i, true);
            }

            return cidrIpAddressBitArray.And(maskBitArray).Cast<bool>().SequenceEqual(ipAddressBitArray.And(maskBitArray).Cast<bool>());
        }
    }
}