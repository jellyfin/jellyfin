using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Jellyfin.Server.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests;

public class WebHostBuilderExtensionsTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(500);

    private static readonly TimeSpan _retryInterval = TimeSpan.FromMilliseconds(50);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateBoundListenSocket_IPv6Any_DualModeFollowsIPv4Setting(bool enableIPv4)
    {
        Assert.SkipUnless(Socket.OSSupportsIPv6, "IPv6 is not supported by the OS");

        using var socket = WebHostBuilderExtensions.CreateBoundListenSocket(
            new IPEndPoint(IPAddress.IPv6Any, 0),
            enableIPv4,
            NullLogger.Instance,
            _timeout,
            _retryInterval);

        Assert.Equal(AddressFamily.InterNetworkV6, socket.AddressFamily);
        Assert.Equal(enableIPv4, socket.DualMode);
    }

    [Fact]
    public void CreateBoundListenSocket_ExplicitIPv6Address_BindsToThatAddress()
    {
        Assert.SkipUnless(Socket.OSSupportsIPv6, "IPv6 is not supported by the OS");

        using var socket = WebHostBuilderExtensions.CreateBoundListenSocket(
            new IPEndPoint(IPAddress.IPv6Loopback, 0),
            false,
            NullLogger.Instance,
            _timeout,
            _retryInterval);

        Assert.Equal(IPAddress.IPv6Loopback, Assert.IsType<IPEndPoint>(socket.LocalEndPoint).Address);
    }

    [Fact]
    public void CreateBoundListenSocket_UnavailableAddress_RetriesUntilTimeout()
    {
        // RFC 5737 TEST-NET-1, which is never assigned to a local interface.
        var endpoint = new IPEndPoint(IPAddress.Parse("192.0.2.1"), 0);
        var startTimestamp = Stopwatch.GetTimestamp();

        var exception = Assert.Throws<SocketException>(() => WebHostBuilderExtensions.CreateBoundListenSocket(
            endpoint,
            true,
            NullLogger.Instance,
            _timeout,
            _retryInterval));

        Assert.Equal(SocketError.AddressNotAvailable, exception.SocketErrorCode);
        Assert.True(Stopwatch.GetElapsedTime(startTimestamp) >= _timeout - _retryInterval);
    }
}
