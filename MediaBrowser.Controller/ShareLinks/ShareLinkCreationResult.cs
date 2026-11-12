using Jellyfin.Database.Implementations.Entities;

namespace MediaBrowser.Controller.ShareLinks;

/// <summary>
/// The result of creating a share link token.
/// </summary>
/// <param name="Token">The PASETO v4.local token, or <c>null</c> when creation failed.</param>
/// <param name="ShareLink">The persisted share link, or <c>null</c> when creation failed.</param>
/// <param name="ErrorCode">The failure reason, or <c>null</c> on success.</param>
public sealed record ShareLinkCreationResult(string? Token, ShareLink? ShareLink, ShareLinkCreateErrorCode? ErrorCode)
{
    /// <summary>
    /// Gets a value indicating whether the token was created successfully.
    /// </summary>
    public bool Succeeded => ErrorCode is null;
}
