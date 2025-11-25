using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Jellyfin.Server.Implementations.Metrics.SessionMetrics
{
    /// <summary>
    /// Provides Prometheus metrics related to Jellyfin sessions.
    /// </summary>
    public class SessionMetrics : IHostedService
    {
        private readonly ILogger<SessionMetrics> _logger;
        private readonly ISessionManager _sessionManager;

        private static readonly Gauge _activeSessions = Prometheus.Metrics
            .CreateGauge("jellyfin_sessions_active_total", "Number of currently active sessions");

        private static readonly Gauge _totalSessions = Prometheus.Metrics
            .CreateGauge("jellyfin_sessions_total", "Total number of sessions, active and inactive");

        private static readonly Gauge _sessionsByClient = Prometheus.Metrics
            .CreateGauge("jellyfin_sessions_by_client", "Number of sessions by client type", new[] { "client" });

        private static readonly Gauge _sessionsByDevice = Prometheus.Metrics
            .CreateGauge("jellyfin_sessions_by_device", "Number of sessions by device model", new[] { "device" });

        private static readonly Gauge _streamsByMethod = Prometheus.Metrics
            .CreateGauge("jellyfin_streams_by_method", "Active streams grouped by playback method", new[] { "method" });

        private static readonly Gauge _transcodesByMode = Prometheus.Metrics
            .CreateGauge("jellyfin_transcodes_by_mode", "Transcodes grouped by software/hardware mode", new[] { "mode" });

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionMetrics"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sessionManager">Instance of <see cref="ISessionManager"/> interface.</param>
        public SessionMetrics(
            ILogger<SessionMetrics> logger,
            ISessionManager sessionManager)
        {
            _logger = logger;
            _sessionManager = sessionManager;

            // Session lifecycle events
            _sessionManager.SessionStarted += OnSessionChanged;
            _sessionManager.SessionEnded += OnSessionChanged;
            _sessionManager.SessionActivity += OnSessionChanged;

            // Playback events
            _sessionManager.PlaybackStart += OnPlaybackChanged;
            _sessionManager.PlaybackProgress += OnPlaybackChanged;
            _sessionManager.PlaybackStopped += OnPlaybackChanged;
            UpdateMetrics();
        }

        private void OnSessionChanged(object? sender, SessionEventArgs args)
        {
            UpdateMetrics();
        }

        private void OnPlaybackChanged(object? sender, PlaybackProgressEventArgs args)
        {
            UpdateMetrics();
        }

        /// <summary>
        /// Updates All Session Metrics.
        /// </summary>
        public void UpdateMetrics()
        {
            var sessions = _sessionManager.Sessions;
            try
            {
                var list = sessions.ToList();

                UpdateSessionCounts(list);
                UpdateClientMetrics(list);
                UpdateDeviceMetrics(list);
                UpdateStreamMetrics(list);
                UpdateTranscodeMetrics(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session metrics");
            }
        }

        /// <summary>
        /// Updates Session Counts Metric.
        /// </summary>
        /// <param name="sessions">List of sessions to update in metrics.</param>
        private void UpdateSessionCounts(IEnumerable<SessionInfo> sessions)
        {
            var list = sessions.ToList();
            _activeSessions.Set(list.Count(s => s.IsActive));
            _totalSessions.Set(list.Count);
        }

        /// <summary>
        /// Updates Sessions' Client Metrics.
        /// </summary>
        /// <param name="sessions">List of sessions to update in metrics.</param>
        private void UpdateClientMetrics(IEnumerable<SessionInfo> sessions)
        {
            var groups = sessions
                .GroupBy(s => s.Client ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kv in groups)
            {
                _sessionsByClient.WithLabels(kv.Key).Set(kv.Value);
            }
        }

        /// <summary>
        /// Updates Sessions' Devices Metric.
        /// </summary>
        /// <param name="sessions">List of sessions to update in metrics.</param>
        private void UpdateDeviceMetrics(IEnumerable<SessionInfo> sessions)
        {
            var groups = sessions
                .GroupBy(s => s.DeviceName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kv in groups)
            {
                _sessionsByDevice.WithLabels(kv.Key).Set(kv.Value);
            }
        }

        /// <summary>
        /// Updates Sessions' Streams Metric.
        /// </summary>
        /// <param name="sessions">List of sessions to update in metrics.</param>
        private void UpdateStreamMetrics(IEnumerable<SessionInfo> sessions)
        {
            var streams = sessions.SelectMany(s => s.NowPlayingItem != null ? new[] { s } : Array.Empty<SessionInfo>());
            var result = streams
                .GroupBy(s => s.PlayState.PlayMethod.ToString() ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kv in result)
            {
                _streamsByMethod.WithLabels(kv.Key).Set(kv.Value);
            }
        }

        /// <summary>
        /// Updates Sessions' Transcode Metric.
        /// </summary>
        /// <param name="sessions">List of sessions to update in metrics.</param>
        private void UpdateTranscodeMetrics(IEnumerable<SessionInfo> sessions)
        {
            var transcodes = sessions.Where(s => s.TranscodingInfo != null);

            var sessionInfos = transcodes as SessionInfo[] ?? transcodes.ToArray();
            var hw = sessionInfos.Count(s => s.TranscodingInfo.HardwareAccelerationType != 0);
            var sw = sessionInfos.Count(s => s.TranscodingInfo.HardwareAccelerationType == 0);

            _transcodesByMode.WithLabels("hardware").Set(hw);
            _transcodesByMode.WithLabels("software").Set(sw);
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
