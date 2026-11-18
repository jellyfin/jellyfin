using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;

namespace Emby.Server.Implementations.Session;

/// <summary>
/// Tracks ownership of live input consumers independently of video admission counts.
/// </summary>
public sealed class LiveStreamPlaybackManager : ILiveStreamPlaybackManager
{
    private static readonly TimeSpan ClosedOwnerRetention = TimeSpan.FromMinutes(5);
    private readonly object _gate = new();
    private readonly IVideoPlaybackCoordinator _coordinator;
    private readonly IMediaSourceManager _mediaSources;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, Binding> _bindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Resource> _resources = new(StringComparer.OrdinalIgnoreCase);
    private long _nextSequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveStreamPlaybackManager"/> class.
    /// </summary>
    /// <param name="coordinator">The video admission registry.</param>
    /// <param name="mediaSources">The live stream resource manager.</param>
    public LiveStreamPlaybackManager(IVideoPlaybackCoordinator coordinator, IMediaSourceManager mediaSources)
        : this(coordinator, mediaSources, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveStreamPlaybackManager"/> class.
    /// </summary>
    /// <param name="coordinator">The video admission registry.</param>
    /// <param name="mediaSources">The live stream resource manager.</param>
    /// <param name="timeProvider">The monotonic clock for closed-owner retention.</param>
    public LiveStreamPlaybackManager(IVideoPlaybackCoordinator coordinator, IMediaSourceManager mediaSources, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(coordinator);
        ArgumentNullException.ThrowIfNull(mediaSources);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _coordinator = coordinator;
        _mediaSources = mediaSources;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public bool Register(VideoPlaybackContext playback, string playSessionId, string liveStreamId, string sessionId, bool isVideo = true)
    {
        ValidateOwner(playback.UserId, playback.SessionId, playback.ItemId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playback.PlaybackInstanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playSessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(liveStreamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        lock (_gate)
        {
            if (_bindings.TryGetValue(playSessionId, out var existing))
            {
                if (existing.Playback.Equals(playback)
                    && string.Equals(existing.LiveStreamId, liveStreamId, StringComparison.OrdinalIgnoreCase)
                    && existing.IsVideo == isVideo)
                {
                    return false;
                }

                throw new SecurityException("The live input identifier belongs to another playback.");
            }

            if (!_resources.TryGetValue(liveStreamId, out var resource))
            {
                resource = new Resource();
                _resources.Add(liveStreamId, resource);
            }

            var binding = new Binding(playback, playSessionId, liveStreamId, sessionId, isVideo, ++_nextSequence);
            resource.GoneAt = null;
            _bindings.Add(playSessionId, binding);
            resource.Bindings.Add(binding);
            var ownerKey = (playback.UserId, playback.SessionId, playback.ItemId);
            if (!resource.Owners.TryGetValue(ownerKey, out var owned))
            {
                owned = [];
                resource.Owners.Add(ownerKey, owned);
            }

            owned.Add(binding);
            resource.ClosedPlaySessions.Remove(playSessionId);
            return true;
        }
    }

    /// <inheritdoc />
    public bool TryGetLiveStream(Guid userId, string ownerId, Guid itemId, string playSessionId, out string? liveStreamId)
    {
        Binding? binding;
        lock (_gate)
        {
            _bindings.TryGetValue(playSessionId, out binding);
        }

        liveStreamId = null;
        if (binding is null || !MatchesOwner(binding, userId, ownerId, itemId)
            || _mediaSources.GetLiveStreamInfo(binding.LiveStreamId) is null)
        {
            return false;
        }

        if (binding.IsVideo && Resolve(binding) != VideoPlaybackStatus.Success)
        {
            return false;
        }

        lock (_gate)
        {
            if (_bindings.TryGetValue(playSessionId, out var current) && ReferenceEquals(current, binding))
            {
                liveStreamId = binding.LiveStreamId;
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool Verify(Guid userId, string ownerId, Guid itemId, string liveStreamId, string? playSessionId = null, bool isVideoOutput = true)
    {
        var candidates = GetOwnedBindings(liveStreamId, userId, ownerId, itemId);
        if (candidates.Length == 0 || _mediaSources.GetLiveStreamInfo(liveStreamId) is null)
        {
            return false;
        }

        if (!isVideoOutput)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(playSessionId))
        {
            return candidates.Any(binding => !binding.IsVideo || Resolve(binding) == VideoPlaybackStatus.Success);
        }

        var status = _coordinator.ResolvePlaySession(userId, ownerId, itemId, playSessionId, out var playback);
        return candidates.Any(binding => !binding.IsVideo
            || (status == VideoPlaybackStatus.Success && binding.Playback.Equals(playback) && Resolve(binding) == VideoPlaybackStatus.Success));
    }

    /// <inheritdoc />
    public bool ContainsPlaySession(string playSessionId)
    {
        lock (_gate)
        {
            return _bindings.ContainsKey(playSessionId);
        }
    }

    /// <inheritdoc />
    public bool VerifyOwner(Guid userId, string ownerId, string liveStreamId, out bool hasOpenConsumer)
    {
        hasOpenConsumer = false;
        lock (_gate)
        {
            if (!_resources.TryGetValue(liveStreamId, out var resource))
            {
                return false;
            }

            hasOpenConsumer = resource.Owners.Keys.Any(owner => owner.UserId.Equals(userId) && string.Equals(owner.OwnerId, ownerId, StringComparison.Ordinal));
            return hasOpenConsumer
                || (resource.ClosedOwners.TryGetValue((userId, ownerId), out var closedAt)
                    && _timeProvider.GetElapsedTime(closedAt, _timeProvider.GetTimestamp()) < ClosedOwnerRetention);
        }
    }

    /// <inheritdoc />
    public Task<bool> ClosePlaybackAsync(string liveStreamId, VideoPlaybackContext playback)
    {
        var stopped = GetBindings(liveStreamId)
            .Where(binding => binding.Playback.Equals(playback)
                && (!binding.IsVideo || Resolve(binding) != VideoPlaybackStatus.Success))
            .ToHashSet();
        return CloseManagedAsync(liveStreamId, stopped.Contains);
    }

    /// <inheritdoc />
    public async Task<bool> CloseSessionAsync(string liveStreamId, string sessionIdOrPlaySessionId)
    {
        var candidates = GetBindings(liveStreamId);
        var selected = candidates.Where(binding => (string.Equals(binding.PlaySessionId, sessionIdOrPlaySessionId, StringComparison.OrdinalIgnoreCase) && CanReleaseInput(binding))
            || (string.Equals(binding.SessionId, sessionIdOrPlaySessionId, StringComparison.OrdinalIgnoreCase)
                && (!binding.IsVideo || Resolve(binding) != VideoPlaybackStatus.Success)))
            .ToHashSet();
        return await CloseManagedAsync(liveStreamId, selected.Contains).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CloseOwnedAsync(Guid userId, string ownerId, string liveStreamId, string? playSessionId = null)
    {
        lock (_gate)
        {
            if (_resources.TryGetValue(liveStreamId, out var resource)
                && ((!string.IsNullOrWhiteSpace(playSessionId)
                        && resource.ClosedPlaySessions.TryGetValue(playSessionId, out var closed)
                        && closed.UserId.Equals(userId)
                        && string.Equals(closed.OwnerId, ownerId, StringComparison.Ordinal))
                    || (string.IsNullOrWhiteSpace(playSessionId) && resource.ClosedOwners.ContainsKey((userId, ownerId)))))
            {
                return;
            }
        }

        var candidates = GetBindings(liveStreamId)
            .Where(binding => binding.Playback.UserId.Equals(userId)
                && string.Equals(binding.Playback.SessionId, ownerId, StringComparison.Ordinal))
            .ToArray();
        if (candidates.Length == 0)
        {
            lock (_gate)
            {
                if (_resources.TryGetValue(liveStreamId, out var resource)
                    && resource.ClosedOwners.ContainsKey((userId, ownerId)))
                {
                    return;
                }
            }

            throw new SecurityException("The live stream does not belong to this playback owner.");
        }

        Binding? selected;
        if (!string.IsNullOrWhiteSpace(playSessionId))
        {
            selected = candidates.FirstOrDefault(binding => string.Equals(binding.PlaySessionId, playSessionId, StringComparison.OrdinalIgnoreCase));
            if (selected is null)
            {
                selected = candidates.FirstOrDefault(binding => binding.IsVideo
                    && _coordinator.ResolvePlaySession(userId, ownerId, binding.Playback.ItemId, playSessionId, out var playback) == VideoPlaybackStatus.Success
                    && binding.Playback.Equals(playback));
            }

            if (selected is null)
            {
                throw new SecurityException("The live input identifier does not belong to this owner.");
            }
        }
        else
        {
            // A legacy close cannot identify one of several independent players.
            if (candidates.Select(binding => binding.Playback).Distinct().Skip(1).Any())
            {
                return;
            }

            selected = candidates.MinBy(binding => binding.Sequence);
            if (!CanReleaseInput(selected!))
            {
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(playSessionId)
            && string.Equals(playSessionId, selected!.PlaySessionId, StringComparison.OrdinalIgnoreCase)
            && !CanReleaseInput(selected))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(playSessionId))
        {
            lock (_gate)
            {
                if (_resources.TryGetValue(liveStreamId, out var resource))
                {
                    resource.ClosedPlaySessions[playSessionId] = new ClosedConsumer(userId, ownerId, _timeProvider.GetTimestamp());
                }
            }
        }

        await CloseManagedAsync(liveStreamId, binding => ReferenceEquals(binding, selected)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetProtectedPlaySessions()
    {
        lock (_gate)
        {
            return new HashSet<string>(_bindings.Keys, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <inheritdoc />
    public Task RemoveExpiredAsync() => RemoveExpiredAsync(Array.Empty<TranscodingJob>());

    /// <inheritdoc />
    public async Task RemoveExpiredAsync(IReadOnlyCollection<TranscodingJob> jobs)
    {
        var audioInputs = jobs
            .Where(job => job.IsOutputVideo == false
                && (!job.HasExited || job.ActiveRequestCount > 0)
                && !string.IsNullOrEmpty(job.PlaybackOwnerId)
                && !string.IsNullOrEmpty(job.LiveStreamId))
            .Select(job => (job.UserId, OwnerId: job.PlaybackOwnerId!, job.ItemId, LiveStreamId: job.LiveStreamId!.ToUpperInvariant()))
            .ToHashSet();
        string[] resources;
        lock (_gate)
        {
            resources = _resources.Keys.ToArray();
        }

        foreach (var liveStreamId in resources)
        {
            var observed = GetBindings(liveStreamId);
            var exists = _mediaSources.GetLiveStreamInfo(liveStreamId) is not null;
            if (!exists)
            {
                lock (_gate)
                {
                    if (_resources.TryGetValue(liveStreamId, out var resource))
                    {
                        var now = _timeProvider.GetTimestamp();
                        foreach (var binding in observed)
                        {
                            if (!resource.Bindings.Remove(binding))
                            {
                                continue;
                            }

                            _bindings.Remove(binding.PlaySessionId);
                            var ownerKey = (binding.Playback.UserId, binding.Playback.SessionId, binding.Playback.ItemId);
                            var owned = resource.Owners[ownerKey];
                            owned.Remove(binding);
                            if (owned.Count == 0)
                            {
                                resource.Owners.Remove(ownerKey);
                            }

                            resource.ClosedOwners[(binding.Playback.UserId, binding.Playback.SessionId)] = now;
                            resource.ClosedPlaySessions[binding.PlaySessionId] = new ClosedConsumer(binding.Playback.UserId, binding.Playback.SessionId, now);
                        }

                        // An Open may register a fresh consumer after the resource lookup.
                        // Only the previously observed consumers belonged to the closed input.
                        if (resource.Bindings.Count == 0)
                        {
                            resource.PendingCloses = 0;
                            resource.GoneAt ??= now;
                            if (_timeProvider.GetElapsedTime(resource.GoneAt.Value, now) >= ClosedOwnerRetention)
                            {
                                _resources.Remove(liveStreamId);
                            }
                        }
                    }
                }

                continue;
            }

            var expired = GetBindings(liveStreamId)
                .Where(binding => binding.IsVideo
                    && !audioInputs.Contains((binding.Playback.UserId, binding.Playback.SessionId, binding.Playback.ItemId, binding.LiveStreamId.ToUpperInvariant()))
                    && Resolve(binding) != VideoPlaybackStatus.Success)
                .ToHashSet();
            await CloseManagedAsync(liveStreamId, expired.Contains).ConfigureAwait(false);
            lock (_gate)
            {
                if (_resources.TryGetValue(liveStreamId, out var resource))
                {
                    var now = _timeProvider.GetTimestamp();
                    foreach (var owner in resource.ClosedOwners.Where(pair => _timeProvider.GetElapsedTime(pair.Value, now) >= ClosedOwnerRetention).Select(pair => pair.Key).ToArray())
                    {
                        resource.ClosedOwners.Remove(owner);
                    }

                    foreach (var alias in resource.ClosedPlaySessions.Where(pair => _timeProvider.GetElapsedTime(pair.Value.Timestamp, now) >= ClosedOwnerRetention).Select(pair => pair.Key).ToArray())
                    {
                        resource.ClosedPlaySessions.Remove(alias);
                    }
                }
            }
        }
    }

    private static void ValidateOwner(Guid userId, string ownerId, Guid itemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        if (userId.Equals(Guid.Empty) || itemId.Equals(Guid.Empty))
        {
            throw new ArgumentException("A live input consumer requires an authenticated user and an item.");
        }
    }

    private static bool MatchesOwner(Binding binding, Guid userId, string ownerId, Guid itemId)
        => binding.Playback.UserId.Equals(userId)
            && binding.Playback.ItemId.Equals(itemId)
            && string.Equals(binding.Playback.SessionId, ownerId, StringComparison.Ordinal);

    private VideoPlaybackStatus Resolve(Binding binding)
        => _coordinator.ResolvePlaySession(binding.Playback.UserId, binding.Playback.SessionId, binding.Playback.ItemId, binding.PlaySessionId, out _);

    private bool CanReleaseInput(Binding binding)
    {
        if (!binding.IsVideo
            || _coordinator.ResolvePlayback(binding.Playback, out var current) != VideoPlaybackStatus.Success
            || string.Equals(current, binding.PlaySessionId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // A new encoding can reuse this consumer without opening the provider again.
        // Release the old consumer only when the replacement owns another input.
        lock (_gate)
        {
            return _bindings.TryGetValue(current!, out var replacement)
                && replacement.Playback.Equals(binding.Playback);
        }
    }

    private Binding[] GetBindings(string liveStreamId)
    {
        lock (_gate)
        {
            return _resources.TryGetValue(liveStreamId, out var resource) ? resource.Bindings.ToArray() : [];
        }
    }

    private Binding[] GetOwnedBindings(string liveStreamId, Guid userId, string ownerId, Guid itemId)
    {
        lock (_gate)
        {
            return _resources.TryGetValue(liveStreamId, out var resource)
                && resource.Owners.TryGetValue((userId, ownerId, itemId), out var bindings)
                    ? bindings.ToArray()
                    : [];
        }
    }

    private async Task<bool> CloseManagedAsync(string liveStreamId, Func<Binding, bool> select)
    {
        lock (_gate)
        {
            if (!_resources.TryGetValue(liveStreamId, out var resource))
            {
                return false;
            }

            var bindings = resource.Bindings.Where(select).ToArray();
            foreach (var binding in bindings)
            {
                resource.Bindings.Remove(binding);
                _bindings.Remove(binding.PlaySessionId);
                var ownerKey = (binding.Playback.UserId, binding.Playback.SessionId, binding.Playback.ItemId);
                var owned = resource.Owners[ownerKey];
                owned.Remove(binding);
                if (owned.Count == 0)
                {
                    resource.Owners.Remove(ownerKey);
                }

                resource.ClosedOwners[(binding.Playback.UserId, binding.Playback.SessionId)] = _timeProvider.GetTimestamp();
                resource.ClosedPlaySessions[binding.PlaySessionId] = new ClosedConsumer(binding.Playback.UserId, binding.Playback.SessionId, _timeProvider.GetTimestamp());
            }

            resource.PendingCloses += bindings.Length;
        }

        while (true)
        {
            lock (_gate)
            {
                if (!_resources.TryGetValue(liveStreamId, out var resource) || resource.PendingCloses == 0)
                {
                    break;
                }

                resource.PendingCloses--;
            }

            try
            {
                await _mediaSources.CloseLiveStream(liveStreamId).ConfigureAwait(false);
            }
            catch
            {
                lock (_gate)
                {
                    if (_resources.TryGetValue(liveStreamId, out var resource))
                    {
                        resource.PendingCloses++;
                    }
                }

                throw;
            }
        }

        return true;
    }

    private readonly record struct ClosedConsumer(Guid UserId, string OwnerId, long Timestamp);

    private sealed record Binding(VideoPlaybackContext Playback, string PlaySessionId, string LiveStreamId, string SessionId, bool IsVideo, long Sequence);

    private sealed class Resource
    {
        public HashSet<Binding> Bindings { get; } = [];

        public Dictionary<(Guid UserId, string OwnerId, Guid ItemId), HashSet<Binding>> Owners { get; } = [];

        public Dictionary<(Guid UserId, string OwnerId), long> ClosedOwners { get; } = [];

        public Dictionary<string, ClosedConsumer> ClosedPlaySessions { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int PendingCloses { get; set; }

        public long? GoneAt { get; set; }
    }
}
