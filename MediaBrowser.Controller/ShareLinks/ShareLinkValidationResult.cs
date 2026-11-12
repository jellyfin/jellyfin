using System;

namespace MediaBrowser.Controller.ShareLinks;

/// <summary>
/// The result of validating a share link token.
/// </summary>
/// <param name="Jti">The token id, or <c>null</c> when validation failed.</param>
/// <param name="UserId">The id of the user that created the token, or <see cref="Guid.Empty"/> when validation failed.</param>
/// <param name="ItemId">The id of the media item the token grants access to, or <see cref="Guid.Empty"/> when validation failed.</param>
/// <param name="IssuedAt">The issuance timestamp in unix seconds, or <c>0</c> when validation failed.</param>
/// <param name="ExpiresAt">The absolute expiration in unix seconds, if any.</param>
/// <param name="DurationSeconds">The duration window in seconds, if any.</param>
/// <param name="ErrorCode">The failure reason, or <c>null</c> on success.</param>
public sealed record ShareLinkValidationResult(
    string? Jti,
    Guid UserId,
    Guid ItemId,
    long IssuedAt,
    long? ExpiresAt,
    int? DurationSeconds,
    ShareLinkErrorCode? ErrorCode)
{
    /// <summary>
    /// Gets a value indicating whether the token is valid.
    /// </summary>
    public bool IsValid => ErrorCode is null;
}
