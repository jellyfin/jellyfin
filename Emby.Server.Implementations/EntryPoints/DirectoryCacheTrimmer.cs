using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints
{
    /// <summary>
    /// <see cref="IHostedService"/> responsible for handing back the memory the directory caches hold.
    /// </summary>
    public sealed class DirectoryCacheTrimmer : IHostedService, IDisposable
    {
        private static readonly TimeSpan _trimInterval = TimeSpan.FromMinutes(5);

        private readonly IDirectoryService _directoryService;
        private readonly ILogger<DirectoryCacheTrimmer> _logger;

        private Timer? _trimTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryCacheTrimmer"/> class.
        /// </summary>
        /// <param name="directoryService">The <see cref="IDirectoryService"/>.</param>
        /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
        public DirectoryCacheTrimmer(IDirectoryService directoryService, ILogger<DirectoryCacheTrimmer> logger)
        {
            _directoryService = directoryService;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _trimTimer = new Timer(OnTrimTimerElapsed, null, _trimInterval, _trimInterval);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _trimTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _trimTimer?.Dispose();
            _trimTimer = null;
        }

        private void OnTrimTimerElapsed(object? state)
        {
            try
            {
                _directoryService.TrimExpired();
            }
            catch (Exception ex)
            {
                // Nothing depends on the trim happening, so a failure must not take the timer down.
                _logger.LogError(ex, "Error trimming the directory caches");
            }
        }
    }
}
