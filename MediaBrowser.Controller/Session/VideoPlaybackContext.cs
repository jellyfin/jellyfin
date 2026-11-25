using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MediaBrowser.Controller.Session;

/// <summary>
/// Identifies a logical video playback and its authenticated owner.
/// </summary>
/// <param name="UserId">The authenticated user identifier.</param>
/// <param name="SessionId">The authenticated, token-bound playback owner identifier.</param>
/// <param name="ItemId">The canonical library item identifier.</param>
/// <param name="PlaybackInstanceId">The stable identifier within the user across encoding changes.</param>
public readonly record struct VideoPlaybackContext(
    Guid UserId,
    string SessionId,
    Guid ItemId,
    string PlaybackInstanceId)
{
    /// <summary>
    /// Creates a canonical fallback identifier for a legacy player without a stable playback identifier.
    /// </summary>
    /// <param name="userId">The authenticated user identifier.</param>
    /// <param name="sessionId">The authenticated, token-bound playback owner identifier.</param>
    /// <param name="itemId">The canonical library item identifier.</param>
    /// <returns>A deterministic identifier scoped to the authenticated user, session and item.</returns>
    /// <remarks>
    /// This compatibility fallback cannot distinguish simultaneous players of the
    /// same item on the same session. It does not replace user authorization.
    /// </remarks>
    public static string CreateLegacyId(Guid userId, string sessionId, Guid itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        var scope = string.Create(CultureInfo.InvariantCulture, $"{userId:N}:{sessionId}:{itemId:N}");
        return "legacy-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));
    }
}
