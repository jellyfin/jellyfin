using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Collects session and stream related Prometheus metrics.
    /// </summary>
    public sealed class SessionMetricsCollector
    {
        private const char LabelSeparator = '\x1f';

        private readonly ISessionManager _sessionManager;
        private readonly HashSet<string> _activeSessionKeys = new();
        private readonly HashSet<string> _activeStreamKeys = new();
        private readonly HashSet<string> _activeTranscodeKeys = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionMetricsCollector"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        public SessionMetricsCollector(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Refreshes session and stream gauges from the current sessions.
        /// </summary>
        public void RefreshGauges()
        {
            var sessions = _sessionManager.Sessions.ToArray();

            JellyfinMetrics.TotalSessions.Set(sessions.Length);

            // Active sessions grouped by device type and client.
            var activeSessions = sessions
                .GroupBy(s => Join(Sanitize(s.DeviceType), Sanitize(s.Client)))
                .ToDictionary(g => g.Key, g => (double)g.Count());
            SetSnapshot(JellyfinMetrics.ActiveSessions, activeSessions, _activeSessionKeys);

            // Active streams grouped by play method.
            var activeStreams = sessions
                .Select(s => s.PlayState?.PlayMethod)
                .Where(playMethod => playMethod.HasValue)
                .GroupBy(playMethod => playMethod.GetValueOrDefault().ToString())
                .ToDictionary(g => g.Key, g => (double)g.Count());
            SetSnapshot(JellyfinMetrics.ActiveStreams, activeStreams, _activeStreamKeys);

            // Active transcodes grouped by acceleration method and codecs.
            var activeTranscodes = sessions
                .Where(s => s.TranscodingInfo is not null)
                .GroupBy(s => Join(
                    Acceleration(s.TranscodingInfo.HardwareAccelerationType),
                    Sanitize(s.TranscodingInfo.VideoCodec),
                    Sanitize(s.TranscodingInfo.AudioCodec)))
                .ToDictionary(g => g.Key, g => (double)g.Count());
            SetSnapshot(JellyfinMetrics.ActiveTranscodes, activeTranscodes, _activeTranscodeKeys);
        }

        /// <summary>
        /// Records counter metrics when a playback stops.
        /// </summary>
        /// <param name="eventArgs">The playback stop event arguments.</param>
        public static void OnPlaybackStopped(PlaybackStopEventArgs eventArgs)
        {
            var session = eventArgs.Session;
            if (session?.PlayState?.PlayMethod is null)
            {
                return;
            }

            var playMethod = session.PlayState.PlayMethod.Value;
            var transcodeReason = session.TranscodingInfo is null
                ? "None"
                : TranscodeReasonLabel(session.TranscodingInfo.TranscodeReasons);
            JellyfinMetrics.PlayMethodsTotal.WithLabels(playMethod.ToString(), transcodeReason).Inc();

            if (playMethod == PlayMethod.Transcode)
            {
                JellyfinMetrics.TranscodesTotal
                    .WithLabels(Acceleration(session.TranscodingInfo?.HardwareAccelerationType))
                    .Inc();
            }
        }

        private static void SetSnapshot(Gauge gauge, Dictionary<string, double> snapshot, HashSet<string> previousKeys)
        {
            foreach (var (key, value) in snapshot)
            {
                gauge.WithLabels(Split(key)).Set(value);
            }

            // Zero out label combinations that disappeared since the last refresh.
            foreach (var key in previousKeys.Where(key => !snapshot.ContainsKey(key)))
            {
                gauge.WithLabels(Split(key)).Set(0);
            }

            previousKeys.Clear();
            previousKeys.UnionWith(snapshot.Keys);
        }

        private static string Acceleration(HardwareAccelerationType? acceleration)
            => acceleration is null or HardwareAccelerationType.none ? "software" : acceleration.Value.ToString();

        private static string TranscodeReasonLabel(TranscodeReason reasons)
            => reasons == 0 ? "None" : reasons.ToString();

        private static string Sanitize(string? value)
            => string.IsNullOrEmpty(value) ? "unknown" : value.Replace(LabelSeparator, '_');

        private static string Join(params string[] values)
            => string.Join(LabelSeparator, values);

        private static string[] Split(string key)
            => key.Split(LabelSeparator);
    }
}
