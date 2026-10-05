using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Streaming;

namespace Jellyfin.Server.Implementations.Security;

/// <inheritdoc />
public sealed class PlaybackAccessManager : IPlaybackAccessManager, IDisposable
{
    private static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan IdleLifetime = TimeSpan.FromHours(4);
    private static readonly TimeSpan StopGracePeriod = TimeSpan.FromSeconds(30);
    private readonly Lock _issuanceLock = new();
    private readonly ConcurrentDictionary<string, Entry> _grants = new(StringComparer.Ordinal);
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly ISessionManager _sessionManager;
    private readonly TimeProvider _timeProvider;
    private readonly ITimer _cleanupTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackAccessManager"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="mediaSourceManager">The media source manager.</param>
    /// <param name="sessionManager">The session manager.</param>
    /// <param name="timeProvider">The time provider.</param>
    public PlaybackAccessManager(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IMediaSourceManager mediaSourceManager,
        ISessionManager sessionManager,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _mediaSourceManager = mediaSourceManager;
        _sessionManager = sessionManager;
        _timeProvider = timeProvider;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _cleanupTimer = timeProvider.CreateTimer(_ => RemoveExpired(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <inheritdoc />
    public async Task<PlaybackAccessGrant> CreateAsync(Guid userId, Guid itemId, string mediaSourceId, string deviceId, string playSessionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaSourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playSessionId);

        var user = _userManager.GetUserById(userId);
        if (!CanPlay(user))
        {
            throw new SecurityException("The user cannot play media.");
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user)
            ?? throw new SecurityException("The user cannot access this item.");
        var sources = await _mediaSourceManager.GetPlaybackMediaSources(item, user!, false, false, cancellationToken).ConfigureAwait(false);
        var source = sources.FirstOrDefault(source => string.Equals(source.Id, mediaSourceId, StringComparison.Ordinal));
        if (source is null || source.RequiresOpening || source.IsInfiniteStream || source.RunTimeTicks.GetValueOrDefault() <= 0)
        {
            throw new SecurityException("The media source is not available for on-demand playback.");
        }

        // Bound memory usage as well as the credentials' lifetime.
        RemoveExpired();
        lock (_issuanceLock)
        {
            if (_grants.Count >= 4096)
            {
                throw new InvalidOperationException("Too many active playback grants.");
            }

            var now = _timeProvider.GetUtcNow();
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var grant = new PlaybackAccessGrant(token, userId, itemId, mediaSourceId, deviceId, playSessionId, now + AbsoluteLifetime);
            _grants[token] = new Entry(grant, now, grant.ExpiresAt);
            return grant;
        }
    }

    /// <inheritdoc />
    public PlaybackAccessGrant? Get(string token)
    {
        if (!_grants.TryGetValue(token, out var entry) || IsExpired(entry, _timeProvider.GetUtcNow()))
        {
            return null;
        }

        var user = _userManager.GetUserById(entry.Grant.UserId);
        if (!CanPlay(user) || _libraryManager.GetItemById<BaseItem>(entry.Grant.ItemId, user) is null)
        {
            _grants.TryRemove(token, out _);
            return null;
        }

        return entry.Grant;
    }

    /// <inheritdoc />
    public void Touch(string token)
    {
        if (_grants.TryGetValue(token, out var entry))
        {
            var now = _timeProvider.GetUtcNow();
            if (!IsExpired(entry, now))
            {
                _grants.TryUpdate(token, entry with { LastAccess = now }, entry);
            }
        }
    }

    /// <inheritdoc />
    public void Revoke(string playSessionId, Guid userId)
    {
        var until = _timeProvider.GetUtcNow() + StopGracePeriod;
        foreach (var pair in _grants)
        {
            var entry = pair.Value;
            if (entry.Grant.UserId.Equals(userId) && string.Equals(entry.Grant.PlaySessionId, playSessionId, StringComparison.Ordinal))
            {
                // A repeated stop must never extend an already revoked grant.
                while (_grants.TryGetValue(pair.Key, out var current) && current.ValidUntil > until)
                {
                    if (_grants.TryUpdate(pair.Key, current with { ValidUntil = until }, current))
                    {
                        break;
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _cleanupTimer.Dispose();
        _grants.Clear();
    }

    private static bool CanPlay(User? user)
        => user is not null
           && !user.HasPermission(PermissionKind.IsDisabled)
           && user.HasPermission(PermissionKind.EnableMediaPlayback)
           && user.IsParentalScheduleAllowed();

    private static bool IsExpired(Entry entry, DateTimeOffset now)
        => now >= entry.ValidUntil || now - entry.LastAccess >= IdleLifetime;

    private void RemoveExpired()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var pair in _grants)
        {
            if (IsExpired(pair.Value, now))
            {
                _grants.TryRemove(pair);
            }
        }
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs args)
    {
        if (string.IsNullOrEmpty(args.PlaySessionId))
        {
            return;
        }

        foreach (var user in args.Users)
        {
            Revoke(args.PlaySessionId, user.Id);
        }
    }

    private sealed record Entry(PlaybackAccessGrant Grant, DateTimeOffset LastAccess, DateTimeOffset ValidUntil);
}
