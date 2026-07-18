using System;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace MediaBrowser.Common.StreamTokens;

/// <summary>
/// Generates and validates self-contained HMAC-SHA256 time-limited stream access tokens.
/// </summary>
/// <remarks>
/// Token format: <c>{base64url(payload)}.{hex(hmac-sha256(secret, payload))}</c>
/// where payload is the UTF-8 encoding of <c>"{itemId}|{expiresUnixSeconds}"</c>.
/// The entire token is passed as a single <see cref="TokenParam"/> query parameter.
/// </remarks>
public static class StreamToken
{
    /// <summary>Query parameter name for the self-contained token.</summary>
    public const string TokenParam = "token";

    /// <summary>
    /// Generates a self-contained, time-limited token for the given item.
    /// </summary>
    /// <param name="secret">The shared secret.</param>
    /// <param name="itemId">The provider-assigned item identifier.</param>
    /// <param name="expiresAt">When the token expires.</param>
    /// <returns>A self-contained token string.</returns>
    public static string Generate(string secret, string itemId, DateTimeOffset expiresAt)
    {
        var payload = BuildPayload(itemId, expiresAt.ToUnixTimeSeconds());
        var sig = Sign(secret, payload);
        return string.Concat(payload, ".", sig);
    }

    /// <summary>
    /// Extracts the item ID and expiry from a token without verifying the signature.
    /// Use <see cref="TryValidate"/> when authentication is required.
    /// </summary>
    /// <param name="token">The token string.</param>
    /// <param name="itemId">The extracted item identifier.</param>
    /// <param name="expiresUnixSeconds">The extracted expiry as a Unix timestamp.</param>
    /// <returns><c>true</c> if the token could be parsed.</returns>
    public static bool TryParse(string token, out string itemId, out long expiresUnixSeconds)
    {
        itemId = string.Empty;
        expiresUnixSeconds = 0;

        var dot = token.IndexOf('.', StringComparison.Ordinal);
        if (dot < 1)
        {
            return false;
        }

        return TryDecodePayload(token[..dot], out itemId, out expiresUnixSeconds);
    }

    /// <summary>
    /// Validates the token signature and expiry, and extracts the item ID.
    /// </summary>
    /// <param name="secret">The shared secret.</param>
    /// <param name="token">The token string.</param>
    /// <param name="now">The current time, used for expiry checking.</param>
    /// <param name="itemId">The item identifier if validation succeeds; otherwise empty.</param>
    /// <returns><c>true</c> if the token is valid and not expired.</returns>
    public static bool TryValidate(string secret, string token, DateTimeOffset now, out string itemId)
    {
        itemId = string.Empty;

        var dot = token.IndexOf('.', StringComparison.Ordinal);
        if (dot < 1)
        {
            return false;
        }

        var payload = token[..dot];
        var receivedSig = token[(dot + 1)..];

        if (!TryDecodePayload(payload, out var parsedItemId, out var expires))
        {
            return false;
        }

        if (now.ToUnixTimeSeconds() > expires)
        {
            return false;
        }

        var expectedSig = Sign(secret, payload);

        // Constant-time comparison to prevent timing attacks.
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(receivedSig.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(expectedSig)))
        {
            return false;
        }

        itemId = parsedItemId;
        return true;
    }

    private static string BuildPayload(string itemId, long expiresUnixSeconds)
    {
        var raw = string.Concat(itemId, "|", expiresUnixSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(raw));
    }

    private static bool TryDecodePayload(string payload, out string itemId, out long expiresUnixSeconds)
    {
        itemId = string.Empty;
        expiresUnixSeconds = 0;

        try
        {
            var raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(payload.AsSpan()));
            var pipe = raw.IndexOf('|', StringComparison.Ordinal);
            if (pipe < 1)
            {
                return false;
            }

            itemId = raw[..pipe];
            return long.TryParse(
                raw[(pipe + 1)..],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out expiresUnixSeconds);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Sign(string secret, string payload)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
