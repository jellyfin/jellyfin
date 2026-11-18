using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Session;

namespace Emby.Server.Implementations.Session;

/// <summary>
/// Tracks video playback admissions and independently owned operations in memory.
/// </summary>
public sealed class VideoPlaybackCoordinator : IVideoPlaybackCoordinator
{
    private static readonly IReadOnlySet<string> _noProtectedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _inactivityTimeout;
    private readonly Dictionary<(Guid UserId, string PlaybackInstanceId), Playback> _playbacks = new();
    private readonly Dictionary<string, PlaySession> _playSessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, int> _activeCounts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoPlaybackCoordinator"/> class.
    /// </summary>
    /// <param name="timeProvider">The monotonic clock.</param>
    /// <param name="inactivityTimeout">The retention period without operations or real activity.</param>
    public VideoPlaybackCoordinator(TimeProvider timeProvider, TimeSpan inactivityTimeout)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(inactivityTimeout, TimeSpan.Zero);
        _timeProvider = timeProvider;
        _inactivityTimeout = inactivityTimeout;
    }

    /// <inheritdoc />
    public VideoPlaybackStatus RegisterPlaySession(VideoPlaybackContext playback, string playSessionId)
    {
        ValidateContext(playback, playSessionId);
        lock (_gate)
        {
            var now = _timeProvider.GetTimestamp();
            if (_playSessions.ContainsKey(playSessionId))
            {
                return Resolve(playback, playSessionId, now, out _);
            }

            var key = (playback.UserId, playback.PlaybackInstanceId);
            if (_playbacks.TryGetValue(key, out var entry))
            {
                if (!Matches(entry.Context, playback))
                {
                    return VideoPlaybackStatus.OwnershipMismatch;
                }

                if (entry.Status != VideoPlaybackStatus.Success)
                {
                    return entry.Status;
                }

                if (IsExpired(entry, now))
                {
                    Retire(entry, VideoPlaybackStatus.Expired, now);
                    return VideoPlaybackStatus.Expired;
                }
            }
            else
            {
                entry = new Playback(playback, now);
                _playbacks.Add(key, entry);
            }

            var session = new PlaySession(entry, ++entry.LastRegistration, playSessionId, now);
            _playSessions.Add(playSessionId, session);
            entry.LastRegisteredSession = session;
            entry.LastActivity = now;
            return VideoPlaybackStatus.Success;
        }
    }

    /// <inheritdoc />
    public VideoPlaybackStatus ResolvePlaySession(
        Guid userId,
        string sessionId,
        Guid? itemId,
        string playSessionId,
        out VideoPlaybackContext playback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playSessionId);
        playback = default;
        lock (_gate)
        {
            if (!_playSessions.TryGetValue(playSessionId, out var session))
            {
                return VideoPlaybackStatus.NotFound;
            }

            var context = session.Playback.Context;
            if (!context.UserId.Equals(userId)
                || !string.Equals(context.SessionId, sessionId, StringComparison.Ordinal)
                || (itemId.HasValue && !context.ItemId.Equals(itemId.Value)))
            {
                return VideoPlaybackStatus.OwnershipMismatch;
            }

            var status = session.RetiredAt.HasValue
                ? VideoPlaybackStatus.Expired
                : ResolveStatus(session.Playback, _timeProvider.GetTimestamp());
            if (status == VideoPlaybackStatus.Success)
            {
                playback = context;
            }

            return status;
        }
    }

    /// <inheritdoc />
    public VideoPlaybackStatus ResolvePlayback(VideoPlaybackContext playback, out string? playSessionId)
    {
        ValidateContext(playback, playback.PlaybackInstanceId);
        playSessionId = null;
        lock (_gate)
        {
            if (!_playbacks.TryGetValue((playback.UserId, playback.PlaybackInstanceId), out var entry))
            {
                return VideoPlaybackStatus.NotFound;
            }

            if (!Matches(entry.Context, playback))
            {
                return VideoPlaybackStatus.OwnershipMismatch;
            }

            var status = ResolveStatus(entry, _timeProvider.GetTimestamp());
            if (status == VideoPlaybackStatus.Success)
            {
                var current = entry.LastAcknowledgedSession;
                foreach (var active in entry.ActiveRegistrations.Values)
                {
                    if (current is null || active.Session.Registration > current.Registration)
                    {
                        current = active.Session;
                    }
                }

                playSessionId = (current ?? entry.LastRegisteredSession)!.Id;
            }

            return status;
        }
    }

    /// <inheritdoc />
    public VideoPlaybackStatus TryAcquire(
        VideoPlaybackContext playback,
        string playSessionId,
        int maxActiveVideoStreams,
        out IVideoPlaybackRequest? request)
    {
        ValidateContext(playback, playSessionId);
        ArgumentOutOfRangeException.ThrowIfNegative(maxActiveVideoStreams);
        request = null;
        lock (_gate)
        {
            var now = _timeProvider.GetTimestamp();
            var status = Resolve(playback, playSessionId, now, out var session);
            if (status != VideoPlaybackStatus.Success)
            {
                return status;
            }

            var entry = session!.Playback;
            if (!entry.Counted)
            {
                var count = _activeCounts.GetValueOrDefault(playback.UserId);
                if (maxActiveVideoStreams > 0 && count >= maxActiveVideoStreams)
                {
                    return VideoPlaybackStatus.LimitReached;
                }

                _activeCounts[playback.UserId] = count + 1;
                entry.Counted = true;
            }

            entry.LastActivity = now;
            session.LastActivity = now;
            var references = entry.ActiveRegistrations.GetValueOrDefault(session.Registration).Count;
            entry.ActiveRegistrations[session.Registration] = (session, references + 1);
            session.HasAcquired = true;
            request = new PlaybackRequest(this, session);
            return VideoPlaybackStatus.Success;
        }
    }

    /// <inheritdoc />
    public VideoPlaybackStatus ReportActivity(VideoPlaybackContext playback, string playSessionId)
    {
        ValidateContext(playback, playSessionId);
        lock (_gate)
        {
            var now = _timeProvider.GetTimestamp();
            var status = Resolve(playback, playSessionId, now, out var session);
            if (status != VideoPlaybackStatus.Success)
            {
                return status;
            }

            if (!session!.Playback.Counted || !session.HasAcquired)
            {
                return VideoPlaybackStatus.NotFound;
            }

            Acknowledge(session, now);
            return VideoPlaybackStatus.Success;
        }
    }

    /// <inheritdoc />
    public VideoPlaybackStatus Stop(VideoPlaybackContext playback, string playSessionId)
    {
        ValidateContext(playback, playSessionId);
        lock (_gate)
        {
            var now = _timeProvider.GetTimestamp();
            var status = Resolve(playback, playSessionId, now, out var session);
            if (status != VideoPlaybackStatus.Success)
            {
                return status;
            }

            var entry = session!.Playback;
            var currentRegistration = entry.LastAcknowledgedRegistration;
            if (!entry.Counted && entry.ActiveRegistrations.Count == 0 && currentRegistration == 0)
            {
                currentRegistration = entry.LastRegisteredSession!.Registration;
            }

            foreach (var registration in entry.ActiveRegistrations.Keys)
            {
                currentRegistration = Math.Max(currentRegistration, registration);
            }

            if (session.Registration < currentRegistration)
            {
                return VideoPlaybackStatus.Superseded;
            }

            if (session.Registration > currentRegistration)
            {
                return VideoPlaybackStatus.NotFound;
            }

            entry.Status = VideoPlaybackStatus.Stopped;
            if (entry.ActiveRegistrations.Count == 0)
            {
                Retire(entry, VideoPlaybackStatus.Stopped, now);
            }

            return VideoPlaybackStatus.Success;
        }
    }

    /// <inheritdoc />
    public int RemoveExpired()
        => RemoveExpired(_noProtectedAliases);

    /// <inheritdoc />
    public int RemoveExpired(IReadOnlySet<string> protectedAliases)
    {
        ArgumentNullException.ThrowIfNull(protectedAliases);
        var protectedSnapshot = protectedAliases.Count == 0
            ? null
            : new HashSet<string>(protectedAliases, StringComparer.OrdinalIgnoreCase);
        lock (_gate)
        {
            var now = _timeProvider.GetTimestamp();
            var expired = new List<Playback>();
            foreach (var entry in _playbacks.Values)
            {
                if (IsExpired(entry, now))
                {
                    expired.Add(entry);
                }
            }

            foreach (var entry in expired)
            {
                Retire(entry, VideoPlaybackStatus.Expired, now);
            }

            var retiredSessions = new List<string>();
            foreach (var pair in _playSessions)
            {
                var alias = pair.Value;
                if (protectedSnapshot?.Contains(pair.Key) == true
                    || alias.Playback.ActiveRegistrations.ContainsKey(alias.Registration))
                {
                    continue;
                }

                var retired = alias.Playback.RetiredAt ?? alias.RetiredAt;
                if (retired is long retiredAt
                    && _timeProvider.GetElapsedTime(retiredAt, now) >= _inactivityTimeout)
                {
                    retiredSessions.Add(pair.Key);
                }
                else if (retired is null
                    && !ReferenceEquals(alias, alias.Playback.LastAcknowledgedSession)
                    && !ReferenceEquals(alias, alias.Playback.LastRegisteredSession)
                    && _timeProvider.GetElapsedTime(alias.LastActivity, now) >= _inactivityTimeout)
                {
                    alias.RetiredAt = now;
                }
            }

            foreach (var playSessionId in retiredSessions)
            {
                _playSessions.Remove(playSessionId);
            }

            return expired.Count;
        }
    }

    private static void ValidateContext(VideoPlaybackContext playback, string playSessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playSessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playback.SessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playback.PlaybackInstanceId);
        if (playback.UserId.Equals(Guid.Empty) || playback.ItemId.Equals(Guid.Empty))
        {
            throw new ArgumentException("Playback requires a user and an item.", nameof(playback));
        }
    }

    private static bool Matches(VideoPlaybackContext left, VideoPlaybackContext right)
        => left.UserId.Equals(right.UserId)
            && left.ItemId.Equals(right.ItemId)
            && string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal)
            && string.Equals(left.PlaybackInstanceId, right.PlaybackInstanceId, StringComparison.Ordinal);

    private static void Acknowledge(PlaySession session, long timestamp)
    {
        session.Playback.Acknowledged = true;
        if (session.Registration > session.Playback.LastAcknowledgedRegistration)
        {
            session.Playback.LastAcknowledgedSession = session;
        }

        session.Playback.LastActivity = timestamp;
        session.LastActivity = timestamp;
    }

    private bool IsExpired(Playback entry, long now)
        => entry.ActiveRegistrations.Count == 0
            && _timeProvider.GetElapsedTime(entry.LastActivity, now) >= _inactivityTimeout;

    private VideoPlaybackStatus Resolve(VideoPlaybackContext playback, string playSessionId, long now, out PlaySession? session)
    {
        if (!_playSessions.TryGetValue(playSessionId, out session))
        {
            return VideoPlaybackStatus.NotFound;
        }

        var entry = session.Playback;
        if (!Matches(entry.Context, playback))
        {
            return VideoPlaybackStatus.OwnershipMismatch;
        }

        if (session.RetiredAt.HasValue)
        {
            return VideoPlaybackStatus.Expired;
        }

        return ResolveStatus(entry, now);
    }

    private VideoPlaybackStatus ResolveStatus(Playback entry, long now)
    {
        if (entry.Status == VideoPlaybackStatus.Success && IsExpired(entry, now))
        {
            Retire(entry, VideoPlaybackStatus.Expired, now);
        }

        return entry.Status;
    }

    private void RemoveCount(Playback entry)
    {
        if (!entry.Counted)
        {
            return;
        }

        var count = _activeCounts[entry.Context.UserId] - 1;
        if (count == 0)
        {
            _activeCounts.Remove(entry.Context.UserId);
        }
        else
        {
            _activeCounts[entry.Context.UserId] = count;
        }

        entry.Counted = false;
    }

    private void Retire(Playback entry, VideoPlaybackStatus status, long now)
    {
        var key = (entry.Context.UserId, entry.Context.PlaybackInstanceId);
        if (_playbacks.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
        {
            _playbacks.Remove(key);
        }

        RemoveCount(entry);
        entry.Status = status;
        entry.RetiredAt = now;
    }

    private void Acknowledge(PlaybackRequest request)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(request.Released, request);
            if (request.Playback.Status == VideoPlaybackStatus.Success)
            {
                Acknowledge(request.PlaySession, _timeProvider.GetTimestamp());
            }
        }
    }

    private IVideoPlaybackRequest Retain(PlaybackRequest request)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(request.Released, request);
            if (request.Playback.Status != VideoPlaybackStatus.Success)
            {
                throw new InvalidOperationException("A stopped playback cannot start another operation.");
            }

            var registrations = request.Playback.ActiveRegistrations;
            var references = registrations[request.PlaySession.Registration].Count;
            registrations[request.PlaySession.Registration] = (request.PlaySession, references + 1);
            return new PlaybackRequest(this, request.PlaySession);
        }
    }

    private void Release(PlaybackRequest request)
    {
        lock (_gate)
        {
            if (request.Released)
            {
                return;
            }

            request.Released = true;
            var entry = request.Playback;
            var registration = request.PlaySession.Registration;
            var references = entry.ActiveRegistrations[registration].Count - 1;
            if (references == 0)
            {
                entry.ActiveRegistrations.Remove(registration);
            }
            else
            {
                entry.ActiveRegistrations[registration] = (request.PlaySession, references);
            }

            entry.LastActivity = _timeProvider.GetTimestamp();
            request.PlaySession.LastActivity = entry.LastActivity;
            if (entry.ActiveRegistrations.Count == 0)
            {
                if (entry.Status == VideoPlaybackStatus.Stopped)
                {
                    Retire(entry, VideoPlaybackStatus.Stopped, entry.LastActivity);
                }
                else if (!entry.Acknowledged)
                {
                    RemoveCount(entry);
                }
            }
        }
    }

    private sealed class Playback(VideoPlaybackContext context, long timestamp)
    {
        public VideoPlaybackContext Context { get; } = context;

        public VideoPlaybackStatus Status { get; set; } = VideoPlaybackStatus.Success;

        public long LastActivity { get; set; } = timestamp;

        public long? RetiredAt { get; set; }

        public long LastRegistration { get; set; }

        public long LastAcknowledgedRegistration => LastAcknowledgedSession?.Registration ?? 0;

        public PlaySession? LastAcknowledgedSession { get; set; }

        public PlaySession? LastRegisteredSession { get; set; }

        public Dictionary<long, (PlaySession Session, int Count)> ActiveRegistrations { get; } = new();

        public bool Counted { get; set; }

        public bool Acknowledged { get; set; }
    }

    private sealed record PlaySession(Playback Playback, long Registration, string Id, long Timestamp)
    {
        public long LastActivity { get; set; } = Timestamp;

        public long? RetiredAt { get; set; }

        public bool HasAcquired { get; set; }
    }

    private sealed class PlaybackRequest(VideoPlaybackCoordinator coordinator, PlaySession playSession) : IVideoPlaybackRequest
    {
        public Playback Playback => PlaySession.Playback;

        public PlaySession PlaySession { get; } = playSession;

        public bool Released { get; set; }

        public void Acknowledge() => coordinator.Acknowledge(this);

        public IVideoPlaybackRequest Retain() => coordinator.Retain(this);

        public void Dispose() => coordinator.Release(this);
    }
}
