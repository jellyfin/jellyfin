#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Class SessionInfo.
    /// </summary>
    public sealed class SessionInfo : SessionInfoModel, IAsyncDisposable, IDisposable
    {
        // 1 second
        private const long ProgressIncrement = 10000000;

        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;

        private readonly object _progressLock = new object();
        private Timer _progressTimer;
        private PlaybackProgressInfo _lastProgressInfo;

        private bool _disposed = false;

        public SessionInfo(ISessionManager sessionManager, ILogger logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
            SessionControllers = Array.Empty<ISessionController>();
        }

        /// <summary>
        /// Gets or sets the session controller.
        /// </summary>
        /// <value>The session controller.</value>
        [JsonIgnore]
        public ISessionController[] SessionControllers { get; set; }

        /// <summary>
        /// Gets or sets the full now playing item.
        /// TODO move this to SessionInfoModel.
        /// </summary>
        public BaseItem FullNowPlayingItem { get; set; }

        /// <inheritdoc />
        public override IReadOnlyList<string> PlayableMediaTypes
        {
            get
            {
                if (Capabilities is null)
                {
                    return Array.Empty<string>();
                }

                return Capabilities.PlayableMediaTypes;
            }
        }

        /// <inheritdoc />
        public override bool IsActive
        {
            get
            {
                var controllers = SessionControllers;
                foreach (var controller in controllers)
                {
                    if (controller.IsSessionActive)
                    {
                        return true;
                    }
                }

                if (controllers.Length > 0)
                {
                    return false;
                }

                return true;
            }
        }

        /// <inheritdoc />
        public override bool SupportsMediaControl
        {
            get
            {
                if (Capabilities is null || !Capabilities.SupportsMediaControl)
                {
                    return false;
                }

                var controllers = SessionControllers;
                foreach (var controller in controllers)
                {
                    if (controller.SupportsMediaControl)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <inheritdoc />
        public override bool SupportsRemoteControl
        {
            get
            {
                if (Capabilities is null || !Capabilities.SupportsMediaControl)
                {
                    return false;
                }

                var controllers = SessionControllers;
                foreach (var controller in controllers)
                {
                    if (controller.SupportsMediaControl)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <inheritdoc />
        public override IReadOnlyList<GeneralCommandType> SupportedCommands
            => Capabilities is null ? Array.Empty<GeneralCommandType>() : Capabilities.SupportedCommands;

        public Tuple<ISessionController, bool> EnsureController<T>(Func<SessionInfo, ISessionController> factory)
        {
            var controllers = SessionControllers.ToList();
            foreach (var controller in controllers)
            {
                if (controller is T)
                {
                    return new Tuple<ISessionController, bool>(controller, false);
                }
            }

            var newController = factory(this);
            _logger.LogDebug("Creating new {0}", newController.GetType().Name);
            controllers.Add(newController);

            SessionControllers = controllers.ToArray();
            return new Tuple<ISessionController, bool>(newController, true);
        }

        public void AddController(ISessionController controller)
        {
            var controllers = SessionControllers.ToList();
            controllers.Add(controller);
            SessionControllers = controllers.ToArray();
        }

        public bool ContainsUser(Guid userId)
        {
            if (UserId.Equals(userId))
            {
                return true;
            }

            foreach (var additionalUser in AdditionalUsers)
            {
                if (additionalUser.UserId.Equals(userId))
                {
                    return true;
                }
            }

            return false;
        }

        public void StartAutomaticProgress(PlaybackProgressInfo progressInfo)
        {
            if (_disposed)
            {
                return;
            }

            lock (_progressLock)
            {
                _lastProgressInfo = progressInfo;

                if (_progressTimer is null)
                {
                    _progressTimer = new Timer(OnProgressTimerCallback, null, 1000, 1000);
                }
                else
                {
                    _progressTimer.Change(1000, 1000);
                }
            }
        }

        private async void OnProgressTimerCallback(object state)
        {
            if (_disposed)
            {
                return;
            }

            var progressInfo = _lastProgressInfo;
            if (progressInfo is null)
            {
                return;
            }

            if (progressInfo.IsPaused)
            {
                return;
            }

            var positionTicks = progressInfo.PositionTicks ?? 0;
            if (positionTicks < 0)
            {
                positionTicks = 0;
            }

            var newPositionTicks = positionTicks + ProgressIncrement;
            var item = progressInfo.Item;
            long? runtimeTicks = item?.RunTimeTicks;

            // Don't report beyond the runtime
            if (runtimeTicks.HasValue && newPositionTicks >= runtimeTicks.Value)
            {
                return;
            }

            progressInfo.PositionTicks = newPositionTicks;

            try
            {
                await _sessionManager.OnPlaybackProgress(progressInfo, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting playback progress");
            }
        }

        public void StopAutomaticProgress()
        {
            lock (_progressLock)
            {
                if (_progressTimer is not null)
                {
                    _progressTimer.Dispose();
                    _progressTimer = null;
                }

                _lastProgressInfo = null;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _disposed = true;

            StopAutomaticProgress();

            var controllers = SessionControllers.ToList();
            SessionControllers = Array.Empty<ISessionController>();

            foreach (var controller in controllers)
            {
                if (controller is IDisposable disposable)
                {
                    _logger.LogDebug("Disposing session controller synchronously {TypeName}", disposable.GetType().Name);
                    disposable.Dispose();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            _disposed = true;

            StopAutomaticProgress();

            var controllers = SessionControllers.ToList();

            foreach (var controller in controllers)
            {
                if (controller is IAsyncDisposable disposableAsync)
                {
                    _logger.LogDebug("Disposing session controller asynchronously {TypeName}", disposableAsync.GetType().Name);
                    await disposableAsync.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
