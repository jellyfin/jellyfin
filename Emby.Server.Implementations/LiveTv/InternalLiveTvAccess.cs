using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace Emby.Server.Implementations.LiveTv;

/// <summary>
/// Issues process-local capabilities for server-owned Live TV input URLs.
/// </summary>
public sealed class InternalLiveTvAccess : IInternalLiveTvAccess
{
    private readonly IServerApplicationHost _applicationHost;
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);

    /// <summary>
    /// Initializes a new instance of the <see cref="InternalLiveTvAccess"/> class.
    /// </summary>
    /// <param name="applicationHost">The server hosting the Live TV input routes.</param>
    public InternalLiveTvAccess(IServerApplicationHost applicationHost)
    {
        ArgumentNullException.ThrowIfNull(applicationHost);
        _applicationHost = applicationHost;
    }

    /// <inheritdoc />
    public string GetInputUrl(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var queryStart = path.IndexOf('?', StringComparison.Ordinal);
        var address = queryStart < 0 ? path : path[..queryStart];
        if (!Uri.TryCreate(path, UriKind.Absolute, out var input)
            || !TryGetLocalApiUri(out var local)
            || !string.Equals(input.Scheme, local.Scheme, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(input.Host, local.Host, StringComparison.OrdinalIgnoreCase)
            || input.Port != local.Port
            || input.UserInfo.Length != 0
            || input.Fragment.Length != 0
            || !HasLiteralPath(address, input.AbsolutePath)
            || !IsResourcePath(input.AbsolutePath, local.AbsolutePath))
        {
            return path;
        }

        if (input.Query.Length != 0)
        {
            var query = QueryHelpers.ParseQuery(input.Query);
            if (query.Count != 1 || !query.ContainsKey(IInternalLiveTvAccess.TokenQueryParameter))
            {
                return path;
            }
        }

        var signature = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(input.AbsolutePath));
        return address + "?" + IInternalLiveTvAccess.TokenQueryParameter + "=" + Convert.ToHexStringLower(signature);
    }

    /// <inheritdoc />
    public bool IsAuthorizedRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var path = request.PathBase.Add(request.Path).Value;
        if (path is null
            || !TryGetLocalApiUri(out var local)
            || !IsResourcePath(path, local.AbsolutePath)
            || !request.Query.TryGetValue(IInternalLiveTvAccess.TokenQueryParameter, out var values)
            || values.Count != 1
            || values[0] is not { Length: 64 } token)
        {
            return false;
        }

        Span<byte> provided = stackalloc byte[32];
        if (Convert.FromHexString(token, provided, out _, out var written) != OperationStatus.Done
            || written != provided.Length)
        {
            return false;
        }

        Span<byte> expected = stackalloc byte[32];
        HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(path), expected);
        return CryptographicOperations.FixedTimeEquals(provided, expected);
    }

    private static bool HasLiteralPath(string address, string path)
    {
        var authorityStart = address.IndexOf("://", StringComparison.Ordinal) + 3;
        var pathStart = address.IndexOf('/', authorityStart);
        return pathStart >= authorityStart
            && address.AsSpan(pathStart).SequenceEqual(path.AsSpan());
    }

    private static bool IsResourcePath(string path, string apiPath)
    {
        var prefix = apiPath.TrimEnd('/');
        if (!path.StartsWith(prefix + "/LiveTv/", StringComparison.Ordinal))
        {
            return false;
        }

        var segments = path[(prefix.Length + 1)..].Split('/');
        if (segments.Length != 4 || segments[2].Length == 0)
        {
            return false;
        }

        foreach (var character in segments[2])
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_'))
            {
                return false;
            }
        }

        return (string.Equals(segments[1], "LiveStreamFiles", StringComparison.Ordinal)
                && string.Equals(segments[3], "stream.ts", StringComparison.Ordinal))
            || (string.Equals(segments[1], "LiveRecordings", StringComparison.Ordinal)
                && string.Equals(segments[3], "stream", StringComparison.Ordinal));
    }

    private bool TryGetLocalApiUri(out Uri uri)
        => Uri.TryCreate(_applicationHost.GetApiUrlForLocalAccess(), UriKind.Absolute, out uri!)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
