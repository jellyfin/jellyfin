using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace MediaBrowser.Common.Net;

/// <summary>
/// Networking constants.
/// </summary>
public static class NetworkConstants
{
    /// <summary>
    /// IPv4 mask bytes.
    /// </summary>
    public const int IPv4MaskBytes = 4;

    /// <summary>
    /// IPv6 mask bytes.
    /// </summary>
    public const int IPv6MaskBytes = 16;

    /// <summary>
    /// Minimum IPv4 prefix size.
    /// </summary>
    public const int MinimumIPv4PrefixSize = 32;

    /// <summary>
    /// Minimum IPv6 prefix size.
    /// </summary>
    public const int MinimumIPv6PrefixSize = 128;

    /// <summary>
    /// Whole IPv4 address space.
    /// </summary>
    public static readonly IPNetwork IPv4Any = new IPNetwork(IPAddress.Any, 0);

    /// <summary>
    /// Whole IPv6 address space.
    /// </summary>
    public static readonly IPNetwork IPv6Any = new IPNetwork(IPAddress.IPv6Any, 0);

    /// <summary>
    /// IPv4 Loopback as defined in RFC 5735.
    /// </summary>
    public static readonly IPNetwork IPv4RFC5735Loopback = new IPNetwork(IPAddress.Loopback, 8);

    /// <summary>
    /// IPv4 private class A as defined in RFC 1918.
    /// </summary>
    public static readonly IPNetwork IPv4RFC1918PrivateClassA = new IPNetwork(IPAddress.Parse("10.0.0.0"), 8);

    /// <summary>
    /// IPv4 private class B as defined in RFC 1918.
    /// </summary>
    public static readonly IPNetwork IPv4RFC1918PrivateClassB = new IPNetwork(IPAddress.Parse("172.16.0.0"), 12);

    /// <summary>
    /// IPv4 private class C as defined in RFC 1918.
    /// </summary>
    public static readonly IPNetwork IPv4RFC1918PrivateClassC = new IPNetwork(IPAddress.Parse("192.168.0.0"), 16);

    /// <summary>
    /// IPv4 Link-Local as defined in RFC 3927.
    /// </summary>
    public static readonly IPNetwork IPv4RFC3927LinkLocal = new IPNetwork(IPAddress.Parse("169.254.0.0"), 16);

    /// <summary>
    /// IPv6 loopback as defined in RFC 4291.
    /// </summary>
    public static readonly IPNetwork IPv6RFC4291Loopback = new IPNetwork(IPAddress.IPv6Loopback, 128);

    /// <summary>
    /// IPv6 site local as defined in RFC 4291.
    /// </summary>
    public static readonly IPNetwork IPv6RFC4291SiteLocal = new IPNetwork(IPAddress.Parse("fe80::"), 10);

    /// <summary>
    /// IPv6 unique local as defined in RFC 4193.
    /// </summary>
    public static readonly IPNetwork IPv6RFC4193UniqueLocal = new IPNetwork(IPAddress.Parse("fc00::"), 7);
}
