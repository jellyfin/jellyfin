/*
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Networking.HappyEyeballs;

/// <summary>
/// Defines the <see cref="HttpClientExtension"/> class.
///
/// Implementation taken from https://github.com/ppy/osu-framework/pull/4191 .
/// </summary>
public static class HttpClientExtension
{
    /// <summary>
    /// Gets or sets a value indicating whether the client should use IPv6.
    /// </summary>
    public static bool UseIPv6 { get; set; } = true;

    /// <summary>
    /// Implements the httpclient callback method.
    /// </summary>
    /// <param name="context">The <see cref="SocketsHttpConnectionContext"/> instance.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> instance.</param>
    /// <returns>The http steam.</returns>
    public static async ValueTask<Stream> OnConnect(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        if (!UseIPv6)
        {
            return await AttemptConnection(AddressFamily.InterNetwork, context, cancellationToken).ConfigureAwait(false);
        }

        using var cancelIPv6 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tryConnectAsyncIPv6 = AttemptConnection(AddressFamily.InterNetworkV6, context, cancelIPv6.Token);

        // GetAwaiter().GetResult() is used instead of .Result as this results in improved exception handling.
        // The tasks have already been completed.
        // See https://github.com/dotnet/corefx/pull/29792/files#r189415885 for more details.
        if (await Task.WhenAny(tryConnectAsyncIPv6, Task.Delay(200, cancelIPv6.Token)).ConfigureAwait(false) == tryConnectAsyncIPv6 && tryConnectAsyncIPv6.IsCompletedSuccessfully)
        {
            await cancelIPv6.CancelAsync().ConfigureAwait(false);
            return tryConnectAsyncIPv6.GetAwaiter().GetResult();
        }

        using var cancelIPv4 = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tryConnectAsyncIPv4 = AttemptConnection(AddressFamily.InterNetwork, context, cancelIPv4.Token);

        if (await Task.WhenAny(tryConnectAsyncIPv6, tryConnectAsyncIPv4).ConfigureAwait(false) == tryConnectAsyncIPv6)
        {
            if (tryConnectAsyncIPv6.IsCompletedSuccessfully)
            {
                await cancelIPv4.CancelAsync().ConfigureAwait(false);
                return tryConnectAsyncIPv6.GetAwaiter().GetResult();
            }

            return tryConnectAsyncIPv4.GetAwaiter().GetResult();
        }
        else
        {
            if (tryConnectAsyncIPv4.IsCompletedSuccessfully)
            {
                await cancelIPv6.CancelAsync().ConfigureAwait(false);
                return tryConnectAsyncIPv4.GetAwaiter().GetResult();
            }

            return tryConnectAsyncIPv6.GetAwaiter().GetResult();
        }
    }

    private static async Task<Stream> AttemptConnection(AddressFamily addressFamily, SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        // The following socket constructor will create a dual-mode socket on systems where IPV6 is available.
        var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp)
        {
            // Turn off Nagle's algorithm since it degrades performance in most HttpClient scenarios.
            NoDelay = true
        };

        try
        {
            var dnsEndPoint = context.DnsEndPoint;

            // Resolve the destination ourselves and connect to the validated address so that the
            // SSRF guard below cannot be bypassed via DNS rebinding (the connection targets the
            // exact address that was checked). Resolving per address family also preserves the
            // Happy Eyeballs fallback behaviour.
            var addresses = await ResolveAddressesAsync(dnsEndPoint.Host, addressFamily, cancellationToken).ConfigureAwait(false);
            if (addresses.Length == 0)
            {
                // Nothing to try for this family; let the other Happy Eyeballs attempt win.
                throw new SocketException((int)SocketError.HostNotFound);
            }

            // Fail closed if any resolved address.
            foreach (var address in addresses)
            {
                if (IsRestrictedAddress(address))
                {
                    throw new HttpRequestException($"Blocked attempt to connect to a restricted address '{address}' for host '{dnsEndPoint.Host}'.");
                }
            }

            await socket.ConnectAsync(addresses, dnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
            // The stream should take the ownership of the underlying socket,
            // closing it when it's disposed.
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task<IPAddress[]> ResolveAddressesAsync(string host, AddressFamily addressFamily, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(host, out var literal))
        {
            // An IPv4-mapped literal is reachable from the IPv6 socket, everything else has to match.
            var usable = literal.AddressFamily == addressFamily
                || (addressFamily == AddressFamily.InterNetworkV6 && literal.AddressFamily == AddressFamily.InterNetwork);
            return usable ? [literal] : [];
        }

        return await Dns.GetHostAddressesAsync(host, addressFamily, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns whether the address must not be connected to.
    /// Unique local addresses (fc00::/7) and RFC1918 ranges are deliberately allowed so LAN tuners
    /// and local metadata services keep working; only host-scoped and link-scoped destinations,
    /// which is where the interesting server-side targets live, are rejected.
    /// </summary>
    /// <param name="address">The resolved destination address.</param>
    /// <returns><c>true</c> if the address must not be connected to; otherwise <c>false</c>.</returns>
    internal static bool IsRestrictedAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] switch
            {
                // "This network" (0.0.0.0/8). 0.0.0.0 reaches the local host on Linux and Windows.
                0 => true,
                // Loopback (127.0.0.0/8).
                127 => true,
                // Link-local (169.254.0.0/16), which contains the 169.254.169.254 cloud metadata endpoint.
                169 when bytes[1] == 254 => true,
                // Multicast (224.0.0.0/4) plus reserved and limited broadcast (240.0.0.0/4).
                >= 224 => true,
                _ => false
            };
        }

        return IPAddress.IsLoopback(address)
            || address.IsIPv6LinkLocal
            || address.IsIPv6SiteLocal
            || address.IsIPv6Multicast
            || IsRestrictedEmbeddedIPv4(address);
    }

    private static bool IsRestrictedEmbeddedIPv4(IPAddress address)
    {
        var bytes = address.GetAddressBytes();

        // 6to4 (2002::/16) carries the IPv4 address in the next four bytes.
        if (bytes[0] == 0x20 && bytes[1] == 0x02)
        {
            return IsRestrictedAddress(new IPAddress(bytes.AsSpan(2, 4)));
        }

        // NAT64 well-known prefix (64:ff9b::/96).
        if (bytes[0] == 0x00 && bytes[1] == 0x64 && bytes[2] == 0xFF && bytes[3] == 0x9B
            && !bytes.AsSpan(4, 8).ContainsAnyExcept((byte)0))
        {
            return IsRestrictedAddress(new IPAddress(bytes.AsSpan(12, 4)));
        }

        // Deprecated IPv4-compatible form (::a.b.c.d). Also catches the unspecified address (::).
        if (!bytes.AsSpan(0, 12).ContainsAnyExcept((byte)0))
        {
            return IsRestrictedAddress(new IPAddress(bytes.AsSpan(12, 4)));
        }

        return false;
    }
}
