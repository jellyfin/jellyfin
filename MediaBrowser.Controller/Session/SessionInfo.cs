#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Class SessionInfo.
    /// </summary>
    public sealed class SessionInfo : IAsyncDisposable
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

            AdditionalUsers = Array.Empty<SessionUserInfo>();
            PlayState = new PlayerStateInfo();
            SessionControllers = Array.Empty<ISessionController>();
            NowPlayingQueue = Array.Empty<QueueItem>();
            NowPlayingQueueFullItems = Array.Empty<BaseItemDto>();
        }

        public PlayerStateInfo PlayState { get; set; }

        public SessionUserInfo[] AdditionalUsers { get; set; }

        public ClientCapabilities Capabilities { get; set; }

        /// <summary>
        /// Gets or sets the remote end point.
        /// </summary>
        /// <value>The remote end point.</value>
        public string RemoteEndPoint { get; set; }

        /// <summary>
        /// Gets the playable media types.
        /// </summary>
        /// <value>The playable media types.</value>
        public IReadOnlyList<MediaType> PlayableMediaTypes
        {
            get
            {
                if (Capabilities is null)
                {
                    return Array.Empty<MediaType>();
                }

                return Capabilities.PlayableMediaTypes;
            }
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>The username.</value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the type of the client.
        /// </summary>
        /// <value>The type of the client.</value>
        public string Client { get; set; }

        /// <summary>
        /// Gets or sets the last activity date.
        /// </summary>
        /// <value>The last activity date.</value>
        public DateTime LastActivityDate { get; set; }

        /// <summary>
        /// Gets or sets the last playback check in.
        /// </summary>
        /// <value>The last playback check in.</value>
        public DateTime LastPlaybackCheckIn { get; set; }

        /// <summary>
        /// Gets or sets the last paused date.
        /// </summary>
        /// <value>The last paused date.</value>
        public DateTime? LastPausedDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the type of the device.
        /// </summary>
        /// <value>The type of the device.</value>
        public string DeviceType { get; set; }

        /// <summary>
        /// Gets or sets the now playing item.
        /// </summary>
        /// <value>The now playing item.</value>
        public BaseItemDto NowPlayingItem { get; set; }

        [JsonIgnore]
        public BaseItem FullNowPlayingItem { get; set; }

        public BaseItemDto NowViewingItem { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        /// <value>The application version.</value>
        public string ApplicationVersion { get; set; }

        /// <summary>
        /// Gets or sets the session controller.
        /// </summary>
        /// <value>The session controller.</value>
        [JsonIgnore]
        public ISessionController[] SessionControllers { get; set; }

        public TranscodingInfo TranscodingInfo { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive
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

        public bool SupportsMediaControl
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

        public bool SupportsRemoteControl
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

        public IReadOnlyList<QueueItem> NowPlayingQueue { get; set; }

        public IReadOnlyList<BaseItemDto> NowPlayingQueueFullItems { get; set; }

        public bool HasCustomDeviceName { get; set; }

        public string PlaylistItemId { get; set; }

        public string ServerId { get; set; }

        public string UserPrimaryImageTag { get; set; }

        /// <summary>
        /// Gets the supported commands.
        /// </summary>
        /// <value>The supported commands.</value>
        public IReadOnlyList<GeneralCommandType> SupportedCommands
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
            SessionControllers = [..SessionControllers, controller];
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

        public async ValueTask DisposeAsync()
        {
            _disposed = true;

            StopAutomaticProgress();

            var controllers = SessionControllers.ToList();
            SessionControllers = Array.Empty<ISessionController>();

            foreach (var controller in controllers)
            {
                if (controller is IAsyncDisposable disposableAsync)
                {
                    _logger.LogDebug("Disposing session controller asynchronously {TypeName}", disposableAsync.GetType().Name);
                    await disposableAsync.DisposeAsync().ConfigureAwait(false);
                }
                else if (controller is IDisposable disposable)
                {
                    _logger.LogDebug("Disposing session controller synchronously {TypeName}", disposable.GetType().Name);
                    disposable.Dispose();
                }
            }
        }
    }
}
