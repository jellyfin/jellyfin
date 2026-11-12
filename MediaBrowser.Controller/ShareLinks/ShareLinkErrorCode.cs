namespace MediaBrowser.Controller.ShareLinks;

/// <summary>
/// The reason a share token was rejected during validation.
/// </summary>
public enum ShareLinkErrorCode
{
    /// <summary>
    /// The token could not be decrypted: it is malformed, tampered with, or was encrypted
    /// with a key that has since been rotated.
    /// </summary>
    DecryptFailed,

    /// <summary>
    /// The requested media item does not match the item scope of the token.
    /// </summary>
    ItemMismatch,

    /// <summary>
    /// The user that created the token does not exist or is disabled.
    /// </summary>
    UserDisabled,

    /// <summary>
    /// The share link backing the token has been revoked.
    /// </summary>
    LinkRevoked,

    /// <summary>
    /// The absolute expiration or the duration window of the share link has passed.
    /// </summary>
    LinkExpired,

    /// <summary>
    /// The share link backing the token no longer exists.
    /// </summary>
    LinkStateMissing
}
