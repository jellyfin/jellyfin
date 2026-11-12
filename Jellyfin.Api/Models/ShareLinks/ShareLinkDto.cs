using System;

namespace Jellyfin.Api.Models.ShareLinks;

/// <summary>
/// A scoped share link.
/// </summary>
public class ShareLinkDto
{
    /// <summary>
    /// Gets or sets the token id (jti) of the share link.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the media item the share link grants access to.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the absolute URL of the shared stream.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp in unix seconds.
    /// </summary>
    public long CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the first access in unix seconds, if any.
    /// </summary>
    public long? FirstAccessedAt { get; set; }

    /// <summary>
    /// Gets or sets the absolute expiration timestamp in unix seconds, if any.
    /// </summary>
    public long? ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the lifetime in seconds starting from the first access, if any.
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the share link has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }
}
