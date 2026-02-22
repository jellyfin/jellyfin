#nullable disable

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

        private readonly Lock _progressLock = new();
        private Timer _progressTimer;
        private PlaybackProgressInfo _lastProgressInfo;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionInfo"/> class.
        /// </summary>
        /// <param name="sessionManager">Instance of <see cref="ISessionManager"/> interface.</param>
        /// <param name="logger">Instance of <see cref="ILogger"/> interface.</param>
        public SessionInfo(ISessionManager sessionManager, ILogger logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;

            AdditionalUsers = [];
            PlayState = new PlayerStateInfo();
            SessionControllers = [];
            NowPlayingQueue = [];
            NowPlayingQueueFullItems = [];
        }

        /// <summary>
        /// Gets or sets the play state.
        /// </summary>
        /// <value>The play state.</value>
        public PlayerStateInfo PlayState { get; set; }

        /// <summary>
        /// Gets or sets the additional users.
        /// </summary>
        /// <value>The additional users.</value>
        public IReadOnlyList<SessionUserInfo> AdditionalUsers { get; set; }

        /// <summary>
        /// Gets or sets the client capabilities.
        /// </summary>
        /// <value>The client capabilities.</value>
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
                    return [];
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

        /// <summary>
        /// Gets or sets the now playing queue full items.
        /// </summary>
        /// <value>The now playing queue full items.</value>
        [JsonIgnore]
        public BaseItem FullNowPlayingItem { get; set; }

        /// <summary>
        /// Gets or sets the now viewing item.
        /// </summary>
        /// <value>The now viewing item.</value>
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
        public IReadOnlyList<ISessionController> SessionControllers { get; set; }

        /// <summary>
        /// Gets or sets the transcoding info.
        /// </summary>
        /// <value>The transcoding info.</value>
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

                if (controllers.Count > 0)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the session supports media control.
        /// </summary>
        /// <value><c>true</c> if this session supports media control; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets a value indicating whether the session supports remote control.
        /// </summary>
        /// <value><c>true</c> if this session supports remote control; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets or sets the now playing queue.
        /// </summary>
        /// <value>The now playing queue.</value>
        public IReadOnlyList<QueueItem> NowPlayingQueue { get; set; }

        /// <summary>
        /// Gets or sets the now playing queue full items.
        /// </summary>
        /// <value>The now playing queue full items.</value>
        public IReadOnlyList<BaseItemDto> NowPlayingQueueFullItems { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the session has a custom device name.
        /// </summary>
        /// <value><c>true</c> if this session has a custom device name; otherwise, <c>false</c>.</value>
        public bool HasCustomDeviceName { get; set; }

        /// <summary>
        /// Gets or sets the playlist item id.
        /// </summary>
        /// <value>The playlist item id.</value>
        public string PlaylistItemId { get; set; }

        /// <summary>
        /// Gets or sets the server id.
        /// </summary>
        /// <value>The server id.</value>
        public string ServerId { get; set; }

        /// <summary>
        /// Gets or sets the user primary image tag.
        /// </summary>
        /// <value>The user primary image tag.</value>
        public string UserPrimaryImageTag { get; set; }

        /// <summary>
        /// Gets the supported commands.
        /// </summary>
        /// <value>The supported commands.</value>
        public IReadOnlyList<GeneralCommandType> SupportedCommands
            => Capabilities is null ? [] : Capabilities.SupportedCommands;

        /// <summary>
        /// Ensures a controller of type exists.
        /// </summary>
        /// <typeparam name="T">Class to register.</typeparam>
        /// <param name="factory">The factory.</param>
        /// <returns>Tuple{ISessionController, bool}.</returns>
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
            _logger.LogDebug("Creating new {Factory}", newController.GetType().Name);
            controllers.Add(newController);

            SessionControllers = [.. controllers];
            return new Tuple<ISessionController, bool>(newController, true);
        }

        /// <summary>
        /// Adds a controller to the session.
        /// </summary>
        /// <param name="controller">The controller.</param>
        public void AddController(ISessionController controller)
        {
            SessionControllers = [.. SessionControllers, controller];
        }

        /// <summary>
        /// Gets a value indicating whether the session contains a user.
        /// </summary>
        /// <param name="userId">The user id to check.</param>
        /// <returns><c>true</c> if this session contains the user; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Starts automatic progressing.
        /// </summary>
        /// <param name="progressInfo">The playback progress info.</param>
        /// <value>The supported commands.</value>
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

        /// <summary>
        /// Stops automatic progressing.
        /// </summary>
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

        /// <summary>
        /// Disposes the instance async.
        /// </summary>
        /// <returns>ValueTask.</returns>
        public async ValueTask DisposeAsync()
        {
            _disposed = true;

            StopAutomaticProgress();

            var controllers = SessionControllers.ToList();
            SessionControllers = [];

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
