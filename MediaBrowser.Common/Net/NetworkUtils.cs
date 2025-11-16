using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Jellyfin.Extensions;
using MediaBrowser.Model.Net;

namespace MediaBrowser.Common.Net;

/// <summary>
/// Defines the <see cref="NetworkUtils" />.
/// </summary>
public static partial class NetworkUtils
{
    // Use regular expression as CheckHostName isn't RFC5892 compliant.
    // Modified from gSkinner's expression at https://stackoverflow.com/questions/11809631/fully-qualified-domain-name-validation
    [GeneratedRegex(@"(?im)^(?!:\/\/)(?=.{1,255}$)((.{1,63}\.){0,127}(?![0-9]*$)[a-z0-9-]+\.?)(:(\d){1,5}){0,1}$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex FqdnGeneratedRegex();

    /// <summary>
    /// Returns true if the IPAddress contains an IP6 Local link address.
    /// </summary>
    /// <param name="address">IPAddress object to check.</param>
    /// <returns>True if it is a local link address.</returns>
    /// <remarks>
    /// See https://stackoverflow.com/questions/6459928/explain-the-instance-properties-of-system-net-ipaddress
    /// it appears that the IPAddress.IsIPv6LinkLocal is out of date.
    /// </remarks>
    public static bool IsIPv6LinkLocal(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        // GetAddressBytes
        Span<byte> octet = stackalloc byte[16];
        address.TryWriteBytes(octet, out _);
        uint word = (uint)(octet[0] << 8) + octet[1];

        return word >= 0xfe80 && word <= 0xfebf; // fe80::/10 :Local link.
    }

    /// <summary>
    /// Convert a subnet mask in CIDR notation to a dotted decimal string value. IPv4 only.
    /// </summary>
    /// <param name="cidr">Subnet mask in CIDR notation.</param>
    /// <param name="family">IPv4 or IPv6 family.</param>
    /// <returns>String value of the subnet mask in dotted decimal notation.</returns>
    public static IPAddress CidrToMask(byte cidr, AddressFamily family)
    {
        uint addr = 0xFFFFFFFF << ((family == AddressFamily.InterNetwork ? NetworkConstants.MinimumIPv4PrefixSize : NetworkConstants.MinimumIPv6PrefixSize) - cidr);
        addr = ((addr & 0xff000000) >> 24)
                | ((addr & 0x00ff0000) >> 8)
                | ((addr & 0x0000ff00) << 8)
                | ((addr & 0x000000ff) << 24);
        return new IPAddress(addr);
    }

    /// <summary>
    /// Convert a subnet mask in CIDR notation to a dotted decimal string value. IPv4 only.
    /// </summary>
    /// <param name="cidr">Subnet mask in CIDR notation.</param>
    /// <param name="family">IPv4 or IPv6 family.</param>
    /// <returns>String value of the subnet mask in dotted decimal notation.</returns>
    public static IPAddress CidrToMask(int cidr, AddressFamily family)
    {
        uint addr = 0xFFFFFFFF << ((family == AddressFamily.InterNetwork ? NetworkConstants.MinimumIPv4PrefixSize : NetworkConstants.MinimumIPv6PrefixSize) - cidr);
        addr = ((addr & 0xff000000) >> 24)
                | ((addr & 0x00ff0000) >> 8)
                | ((addr & 0x0000ff00) << 8)
                | ((addr & 0x000000ff) << 24);
        return new IPAddress(addr);
    }

    /// <summary>
    /// Convert a subnet mask to a CIDR. IPv4 only.
    /// https://stackoverflow.com/questions/36954345/get-cidr-from-netmask.
    /// </summary>
    /// <param name="mask">Subnet mask.</param>
    /// <returns>Byte CIDR representing the mask.</returns>
    public static byte MaskToCidr(IPAddress mask)
    {
        ArgumentNullException.ThrowIfNull(mask);

        byte cidrnet = 0;
        if (mask.Equals(IPAddress.Any))
        {
            return cidrnet;
        }

        // GetAddressBytes
        Span<byte> bytes = stackalloc byte[mask.AddressFamily == AddressFamily.InterNetwork ? NetworkConstants.IPv4MaskBytes : NetworkConstants.IPv6MaskBytes];
        if (!mask.TryWriteBytes(bytes, out var bytesWritten))
        {
            Console.WriteLine("Unable to write address bytes, only {0} bytes written.", bytesWritten.ToString(CultureInfo.InvariantCulture));
        }

        var zeroed = false;
        for (var i = 0; i < bytes.Length; i++)
        {
            for (int v = bytes[i]; (v & 0xFF) != 0; v <<= 1)
            {
                if (zeroed)
                {
                    // Invalid netmask.
                    return (byte)~cidrnet;
                }

                if ((v & 0x80) == 0)
                {
                    zeroed = true;
                }
                else
                {
                    cidrnet++;
                }
            }
        }

        return cidrnet;
    }

    /// <summary>
    /// Converts an IPAddress into a string.
    /// IPv6 addresses are returned in [ ], with their scope removed.
    /// </summary>
    /// <param name="address">Address to convert.</param>
    /// <returns>URI safe conversion of the address.</returns>
    public static string FormatIPString(IPAddress? address)
    {
        if (address is null)
        {
            return string.Empty;
        }

        var str = address.ToString();
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            int i = str.IndexOf('%', StringComparison.Ordinal);
            if (i != -1)
            {
                str = str.Substring(0, i);
            }

            return $"[{str}]";
        }

        return str;
    }

    /// <summary>
    /// Try parsing an array of strings into <see cref="IPNetwork"/> objects, respecting exclusions.
    /// Elements without a subnet mask will be represented as <see cref="IPNetwork"/> with a single IP.
    /// </summary>
    /// <param name="values">Input string array to be parsed.</param>
    /// <param name="result">Collection of <see cref="IPNetwork"/>.</param>
    /// <param name="negated">Boolean signaling if negated or not negated values should be parsed.</param>
    /// <returns><c>True</c> if parsing was successful.</returns>
    public static bool TryParseToSubnets(string[] values, [NotNullWhen(true)] out IReadOnlyList<IPData>? result, bool negated = false)
    {
        if (values is null || values.Length == 0)
        {
            result = null;
            return false;
        }

        List<IPData>? tmpResult = null;
        for (int a = 0; a < values.Length; a++)
        {
            if (TryParseToSubnet(values[a], out var innerResult, negated))
            {
                (tmpResult ??= new()).Add(innerResult);
            }
        }

        result = tmpResult;
        return result is not null;
    }

    /// <summary>
    /// Try parsing a string into an <see cref="IPData"/>, respecting exclusions.
    /// Inputs without a subnet mask will be represented as <see cref="IPData"/> with a single IP.
    /// </summary>
    /// <param name="value">Input string to be parsed.</param>
    /// <param name="result">An <see cref="IPData"/>.</param>
    /// <param name="negated">Boolean signaling if negated or not negated values should be parsed.</param>
    /// <returns><c>True</c> if parsing was successful.</returns>
    public static bool TryParseToSubnet(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPData? result, bool negated = false)
    {
        // If multiple IP addresses are in a comma-separated string, the individual addresses may contain leading and/or trailing whitespace
        value = value.Trim();

        bool isAddressNegated = false;
        if (value.StartsWith('!'))
        {
            isAddressNegated = true;
            value = value[1..]; // Remove leading '!' character
        }

        if (isAddressNegated != negated)
        {
            result = default;
            return false;
        }

        var index = value.IndexOf('/');
        if (index != -1)
        {
            if (IPAddress.TryParse(value[..index], out var address) && IPNetwork.TryParse(value, out var subnet))
            {
                result = new IPData(address, subnet);
                return true;
            }
        }
        else if (IPAddress.TryParse(value, out var address))
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                result = address.Equals(IPAddress.Any) ? new IPData(IPAddress.Any, NetworkConstants.IPv4Any) : new IPData(address, new IPNetwork(address, NetworkConstants.MinimumIPv4PrefixSize));
                return true;
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                result = address.Equals(IPAddress.IPv6Any) ? new IPData(IPAddress.IPv6Any, NetworkConstants.IPv6Any) : new IPData(address, new IPNetwork(address, NetworkConstants.MinimumIPv6PrefixSize));
                return true;
            }
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Attempts to parse a host span.
    /// </summary>
    /// <param name="host">Host name to parse.</param>
    /// <param name="addresses">Object representing the span, if it has successfully been parsed.</param>
    /// <param name="isIPv4Enabled"><c>true</c> if IPv4 is enabled.</param>
    /// <param name="isIPv6Enabled"><c>true</c> if IPv6 is enabled.</param>
    /// <returns><c>true</c> if the parsing is successful, <c>false</c> if not.</returns>
    public static bool TryParseHost(ReadOnlySpan<char> host, [NotNullWhen(true)] out IPAddress[]? addresses, bool isIPv4Enabled = true, bool isIPv6Enabled = false)
    {
        host = host.Trim();
        if (host.IsEmpty)
        {
            addresses = null;
            return false;
        }

        // See if it's an IPv6 with port address e.g. [::1] or [::1]:120.
        if (host[0] == '[')
        {
            int i = host.IndexOf(']');
            if (i != -1)
            {
                return TryParseHost(host[1..(i - 1)], out addresses);
            }

            addresses = Array.Empty<IPAddress>();
            return false;
        }

        var hosts = new List<string>();
        foreach (var splitSpan in host.Split(':'))
        {
            hosts.Add(splitSpan.ToString());
        }

        if (hosts.Count <= 2)
        {
            var firstPart = hosts[0];

            // Is hostname or hostname:port
            if (FqdnGeneratedRegex().IsMatch(firstPart))
            {
                try
                {
                    // .NET automatically filters only supported returned addresses based on OS support.
                    addresses = Dns.GetHostAddresses(firstPart);
                    return true;
                }
                catch (SocketException)
                {
                    // Ignore socket errors, as the result value will just be an empty array.
                }
            }

            // Is an IPv4 or IPv4:port
            if (IPAddress.TryParse(firstPart.AsSpan().LeftPart('/'), out var address))
            {
                if (((address.AddressFamily == AddressFamily.InterNetwork) && (!isIPv4Enabled && isIPv6Enabled))
                    || ((address.AddressFamily == AddressFamily.InterNetworkV6) && (isIPv4Enabled && !isIPv6Enabled)))
                {
                    addresses = Array.Empty<IPAddress>();
                    return false;
                }

                addresses = new[] { address };

                // Host name is an IPv4 address, so fake resolve.
                return true;
            }
        }
        else if (hosts.Count > 0 && hosts.Count <= 9) // 8 octets + port
        {
            if (IPAddress.TryParse(host.LeftPart('/'), out var address))
            {
                addresses = new[] { address };
                return true;
            }
        }

        addresses = Array.Empty<IPAddress>();
        return false;
    }

    /// <summary>
    /// Gets the broadcast address for a <see cref="IPNetwork"/>.
    /// </summary>
    /// <param name="network">The <see cref="IPNetwork"/>.</param>
    /// <returns>The broadcast address.</returns>
    public static IPAddress GetBroadcastAddress(IPNetwork network)
    {
        var addressBytes = network.BaseAddress.GetAddressBytes();
        uint ipAddress = BitConverter.ToUInt32(addressBytes, 0);
        uint ipMaskV4 = BitConverter.ToUInt32(CidrToMask(network.PrefixLength, AddressFamily.InterNetwork).GetAddressBytes(), 0);
        uint broadCastIPAddress = ipAddress | ~ipMaskV4;

        return new IPAddress(BitConverter.GetBytes(broadCastIPAddress));
    }

    /// <summary>
    /// Check if a subnet contains an address. This method also handles IPv4 mapped to IPv6 addresses.
    /// </summary>
    /// <param name="network">The <see cref="IPNetwork"/>.</param>
    /// <param name="address">The <see cref="IPAddress"/>.</param>
    /// <returns>Whether the supplied IP is in the supplied network.</returns>
    public static bool SubnetContainsAddress(IPNetwork network, IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return network.Contains(address);
    }
}
