using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Streaming;

namespace Jellyfin.Server.Implementations.Security;

/// <inheritdoc />
public class PlaybackAccessManager : IPlaybackAccessManager
{
    private static readonly TimeSpan _lifetime = TimeSpan.FromHours(24);

    private readonly ConcurrentDictionary<string, PlaybackAccessGrant> _grants = new(StringComparer.Ordinal);
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaSourceManager _mediaSourceManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackAccessManager"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    public PlaybackAccessManager(IUserManager userManager, ILibraryManager libraryManager, IMediaSourceManager mediaSourceManager)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _mediaSourceManager = mediaSourceManager;
    }

    /// <inheritdoc />
    public async Task<PlaybackAccessGrant> CreateAsync(Guid userId, Guid itemId, string mediaSourceId, string deviceId, string playSessionId, CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null
            || user.HasPermission(PermissionKind.IsDisabled)
            || !user.HasPermission(PermissionKind.EnableMediaPlayback)
            || !user.IsParentalScheduleAllowed())
        {
            throw new SecurityException("The user is not allowed to play media.");
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user)
            ?? throw new SecurityException("The user is not allowed to access the item.");
        var mediaSources = await _mediaSourceManager.GetPlaybackMediaSources(item, user, false, false, cancellationToken).ConfigureAwait(false);
        if (!mediaSources.Any(i => string.Equals(i.Id, mediaSourceId, StringComparison.Ordinal)))
        {
            throw new SecurityException("The media source does not belong to the item.");
        }

        var now = DateTime.UtcNow;
        foreach (var expired in _grants.Where(i => i.Value.ExpiresAt <= now))
        {
            _grants.TryRemove(expired);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var grant = new PlaybackAccessGrant(token, userId, itemId, mediaSourceId, deviceId, playSessionId, now.Add(_lifetime));
        _grants[token] = grant;
        return grant;
    }

    /// <inheritdoc />
    public PlaybackAccessGrant? Get(string token)
    {
        if (!_grants.TryGetValue(token, out var grant) || grant.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var user = _userManager.GetUserById(grant.UserId);
        return user is null || user.HasPermission(PermissionKind.IsDisabled) ? null : grant;
    }

    /// <inheritdoc />
    public void Revoke(string playSessionId, Guid userId)
    {
        foreach (var grant in _grants.Where(i => i.Value.UserId.Equals(userId) && string.Equals(i.Value.PlaySessionId, playSessionId, StringComparison.Ordinal)))
        {
            _grants.TryRemove(grant);
        }
    }
}
