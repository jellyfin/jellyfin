using System;
using System.Security.Cryptography;
using System.Text;

namespace MediaBrowser.Common.StreamTokens;

/// <summary>
/// Generates and validates HMAC-SHA256 time-limited stream access tokens.
/// </summary>
/// <remarks>
/// Token format: HMAC-SHA256(secret, "{itemId}|{expiresUnixSeconds}") as lowercase hex.
/// The caller passes the token and expiry as the query parameters <see cref="TokenParam"/>
/// and <see cref="ExpiresParam"/> on the redirect URL.
/// </remarks>
public static class StreamToken
{
    /// <summary>Query parameter name for the HMAC token.</summary>
    public const string TokenParam = "token";

    /// <summary>Query parameter name for the expiry unix timestamp (seconds).</summary>
    public const string ExpiresParam = "expires";

    /// <summary>
    /// Generates a time-limited HMAC-SHA256 token for the given item.
    /// </summary>
    /// <param name="secret">The shared secret.</param>
    /// <param name="itemId">The provider-assigned item identifier.</param>
    /// <param name="expiresAt">When the token expires.</param>
    /// <returns>The lowercase hex-encoded HMAC token.</returns>
    public static string Generate(string secret, string itemId, DateTimeOffset expiresAt)
    {
        var message = BuildMessage(itemId, expiresAt.ToUnixTimeSeconds());
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(message));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Validates a token against the expected item and expiry.
    /// </summary>
    /// <param name="secret">The shared secret.</param>
    /// <param name="token">The token from the request.</param>
    /// <param name="itemId">The provider-assigned item identifier.</param>
    /// <param name="expiresUnixSeconds">The expiry unix timestamp from the request.</param>
    /// <param name="now">The current time, used for expiry checking.</param>
    /// <returns><c>true</c> if the token signature is valid and the token has not expired.</returns>
    public static bool Validate(
        string secret,
        string token,
        string itemId,
        long expiresUnixSeconds,
        DateTimeOffset now)
    {
        if (now.ToUnixTimeSeconds() > expiresUnixSeconds)
        {
            return false;
        }

        var expected = Generate(secret, itemId, DateTimeOffset.FromUnixTimeSeconds(expiresUnixSeconds));

        // Constant-time comparison to prevent timing attacks.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(token.ToLowerInvariant()),
            Encoding.UTF8.GetBytes(expected));
    }

    private static string BuildMessage(string itemId, long expiresUnixSeconds)
        => string.Concat(itemId, "|", expiresUnixSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
