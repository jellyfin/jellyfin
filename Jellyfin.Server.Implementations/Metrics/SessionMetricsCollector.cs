using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Collects session and stream related Prometheus metrics.
    /// </summary>
    public sealed class SessionMetricsCollector
    {
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
                .GroupBy(s => MetricsSnapshot.Join(MetricsSnapshot.Sanitize(s.DeviceType), MetricsSnapshot.Sanitize(s.Client)))
                .ToDictionary(g => g.Key, g => (double)g.Count());
            MetricsSnapshot.SetSnapshot(JellyfinMetrics.ActiveSessions, activeSessions, _activeSessionKeys);

            // Active streams grouped by play method.
            var activeStreams = sessions
                .Select(s => s.PlayState?.PlayMethod)
                .Where(playMethod => playMethod.HasValue)
                .GroupBy(playMethod => playMethod.GetValueOrDefault().ToString())
                .ToDictionary(g => g.Key, g => (double)g.Count());
            MetricsSnapshot.SetSnapshot(JellyfinMetrics.ActiveStreams, activeStreams, _activeStreamKeys);

            // Active transcodes grouped by acceleration method and codecs.
            var activeTranscodes = sessions
                .Where(s => s.TranscodingInfo is not null)
                .GroupBy(s => MetricsSnapshot.Join(
                    Acceleration(s.TranscodingInfo.HardwareAccelerationType),
                    MetricsSnapshot.Sanitize(s.TranscodingInfo.VideoCodec),
                    MetricsSnapshot.Sanitize(s.TranscodingInfo.AudioCodec)))
                .ToDictionary(g => g.Key, g => (double)g.Count());
            MetricsSnapshot.SetSnapshot(JellyfinMetrics.ActiveTranscodes, activeTranscodes, _activeTranscodeKeys);
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

        private static string Acceleration(HardwareAccelerationType? acceleration)
            => acceleration is null or HardwareAccelerationType.none ? "software" : acceleration.Value.ToString();

        private static string TranscodeReasonLabel(TranscodeReason reasons)
            => reasons == 0 ? "None" : reasons.ToString();
    }
}
