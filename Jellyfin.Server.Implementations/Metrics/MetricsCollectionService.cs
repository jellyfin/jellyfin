using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Metrics
{
    /// <summary>
    /// Periodically refreshes Jellyfin-specific Prometheus gauges from live server state.
    /// </summary>
    /// <remarks>
    /// Gauges are refreshed on a timer so that scraping the <c>/metrics</c> endpoint stays cheap and the
    /// playback/session hot path is never blocked by metric collection.
    /// </remarks>
    public sealed class MetricsCollectionService : IHostedService, IDisposable
    {
        private static readonly TimeSpan _collectionInterval = TimeSpan.FromSeconds(15);

        private readonly ISessionManager _sessionManager;
        private readonly SessionMetricsCollector _sessionMetricsCollector;
        private readonly LibraryMetricsCollector _libraryMetricsCollector;
        private readonly UserMetricsCollector _userMetricsCollector;
        private readonly AuthenticationMetricsCollector _authenticationMetricsCollector;
        private readonly ILogger<MetricsCollectionService> _logger;
        private Timer? _timer;
        private bool _disposed;
        private int _isRefreshing;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCollectionService"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager.</param>
        /// <param name="sessionMetricsCollector">The session metrics collector.</param>
        /// <param name="libraryMetricsCollector">The library metrics collector.</param>
        /// <param name="userMetricsCollector">The user metrics collector.</param>
        /// <param name="authenticationMetricsCollector">The authentication metrics collector.</param>
        /// <param name="logger">The logger.</param>
        public MetricsCollectionService(
            ISessionManager sessionManager,
            SessionMetricsCollector sessionMetricsCollector,
            LibraryMetricsCollector libraryMetricsCollector,
            UserMetricsCollector userMetricsCollector,
            AuthenticationMetricsCollector authenticationMetricsCollector,
            ILogger<MetricsCollectionService> logger)
        {
            _sessionManager = sessionManager;
            _sessionMetricsCollector = sessionMetricsCollector;
            _libraryMetricsCollector = libraryMetricsCollector;
            _userMetricsCollector = userMetricsCollector;
            _authenticationMetricsCollector = authenticationMetricsCollector;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Jellyfin metrics collection");

            _sessionManager.PlaybackStart += OnPlaybackStarted;

            // Refresh immediately so values are available without waiting for the first interval.
            RefreshGauges();

            _timer = new Timer(_ => RefreshGauges(), null, _collectionInterval, _collectionInterval);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _sessionManager.PlaybackStart -= OnPlaybackStarted;
            var timer = Interlocked.Exchange(ref _timer, null);
            if (timer is not null)
            {
                await timer.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Interlocked.Exchange(ref _timer, null)?.Dispose();
            _disposed = true;
        }

        private void OnPlaybackStarted(object? sender, PlaybackProgressEventArgs eventArgs)
        {
            try
            {
                SessionMetricsCollector.OnPlaybackStarted(eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to record playback start metrics");
            }
        }

        private void RefreshGauges()
        {
            if (Interlocked.Exchange(ref _isRefreshing, 1) != 0)
            {
                return;
            }

            try
            {
                _sessionMetricsCollector.RefreshGauges();
                _libraryMetricsCollector.RefreshGauges();
                _userMetricsCollector.RefreshGauges();
                _authenticationMetricsCollector.RefreshGauges();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh Jellyfin metrics");
            }
            finally
            {
                Volatile.Write(ref _isRefreshing, 0);
            }
        }
    }
}
