using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Session;

namespace Emby.Server.Implementations.Session;

/// <summary>
/// Tracks video playback admissions and independently owned operations in memory.
/// </summary>
public sealed class VideoPlaybackCoordinator : IVideoPlaybackCoordinator
{
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _inactivityTimeout;
    private readonly Dictionary<(Guid UserId, string PlaybackInstanceId), Playback> _playbacks = new();
    private readonly Dictionary<string, PlaySession> _playSessions = new(StringComparer.Ordinal);
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

            _playSessions.Add(playSessionId, new PlaySession(entry, ++entry.LastRegistration));
            return VideoPlaybackStatus.Success;
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
            entry.References++;
            request = new PlaybackRequest(this, entry, session.Registration);
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

            if (!session!.Playback.Counted)
            {
                return VideoPlaybackStatus.NotFound;
            }

            session.Playback.LastActivity = now;
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
            if (session.Registration < entry.LastAcknowledgedRegistration)
            {
                return VideoPlaybackStatus.Superseded;
            }

            if (session.Registration > entry.LastAcknowledgedRegistration)
            {
                return VideoPlaybackStatus.NotFound;
            }

            entry.Status = VideoPlaybackStatus.Stopped;
            if (entry.References == 0)
            {
                Retire(entry, VideoPlaybackStatus.Stopped, now);
            }

            return VideoPlaybackStatus.Success;
        }
    }

    /// <inheritdoc />
    public int RemoveExpired()
    {
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
                if (pair.Value.Playback.RetiredAt is long retiredAt
                    && _timeProvider.GetElapsedTime(retiredAt, now) >= _inactivityTimeout)
                {
                    retiredSessions.Add(pair.Key);
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

    private bool IsExpired(Playback entry, long now)
        => entry.References == 0
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

        if (entry.Status != VideoPlaybackStatus.Success)
        {
            return entry.Status;
        }

        if (IsExpired(entry, now))
        {
            Retire(entry, VideoPlaybackStatus.Expired, now);
            return VideoPlaybackStatus.Expired;
        }

        return VideoPlaybackStatus.Success;
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
                request.Playback.Acknowledged = true;
                request.Playback.LastAcknowledgedRegistration = Math.Max(request.Playback.LastAcknowledgedRegistration, request.Registration);
                request.Playback.LastActivity = _timeProvider.GetTimestamp();
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

            request.Playback.References++;
            return new PlaybackRequest(this, request.Playback, request.Registration);
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
            entry.References--;
            entry.LastActivity = _timeProvider.GetTimestamp();
            if (entry.References == 0)
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

        public long LastAcknowledgedRegistration { get; set; }

        public int References { get; set; }

        public bool Counted { get; set; }

        public bool Acknowledged { get; set; }
    }

    private sealed record PlaySession(Playback Playback, long Registration);

    private sealed class PlaybackRequest(VideoPlaybackCoordinator coordinator, Playback playback, long registration) : IVideoPlaybackRequest
    {
        public Playback Playback { get; } = playback;

        public long Registration { get; } = registration;

        public bool Released { get; set; }

        public void Acknowledge() => coordinator.Acknowledge(this);

        public IVideoPlaybackRequest Retain() => coordinator.Retain(this);

        public void Dispose() => coordinator.Release(this);
    }
}
