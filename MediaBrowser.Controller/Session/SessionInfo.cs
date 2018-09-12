using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Threading;
using System.Linq;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Class SessionInfo
    /// </summary>
    public class SessionInfo : IDisposable
    {
        private ISessionManager _sessionManager;
        private readonly ILogger _logger;

        public SessionInfo(ISessionManager sessionManager, ILogger logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;

            AdditionalUsers = new SessionUserInfo[] { };
            PlayState = new PlayerStateInfo();
            SessionControllers = new ISessionController[] { };
        }

        // This is only here to allow XmlSerialization and should not be used
        public SessionInfo() : this(null, null) { }

        public PlayerStateInfo PlayState { get; set; }

        public SessionUserInfo[] AdditionalUsers { get; set; }

        [IgnoreDataMember]
        public QueueItem[] NowPlayingQueue { get; set; }

        [IgnoreDataMember]
        public bool HasCustomDeviceName { get; set; }

        public ClientCapabilities Capabilities { get; set; }

        /// <summary>
        /// Gets or sets the remote end point.
        /// </summary>
        /// <value>The remote end point.</value>
        public string RemoteEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the playable media types.
        /// </summary>
        /// <value>The playable media types.</value>
        public string[] PlayableMediaTypes
        {
            get
            {
                var caps = Capabilities;
                if (caps == null)
                {
                    return Array.Empty<string>();
                }
                return caps.PlayableMediaTypes;
            }
        }

        public string PlaylistItemId { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        public string ServerId { get; set; }

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

        public string UserPrimaryImageTag { get; set; }

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
        [IgnoreDataMember]
        public DateTime LastPlaybackCheckIn { get; set; }

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName { get; set; }

        public string DeviceType { get; set; }

        /// <summary>
        /// Gets or sets the now playing item.
        /// </summary>
        /// <value>The now playing item.</value>
        public BaseItemDto NowPlayingItem { get; set; }

        [IgnoreDataMember]
        public BaseItem FullNowPlayingItem { get; set; }

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
        [IgnoreDataMember]
        public ISessionController[] SessionControllers { get; set; }

        /// <summary>
        /// Gets or sets the application icon URL.
        /// </summary>
        /// <value>The application icon URL.</value>
        public string AppIconUrl
        {
            get
            {
                var caps = Capabilities;
                return caps == null ? null : caps.IconUrl;
            }
        }

        /// <summary>
        /// Gets or sets the supported commands.
        /// </summary>
        /// <value>The supported commands.</value>
        public string[] SupportedCommands
        {
            get
            {
                var caps = Capabilities;
                if (caps == null)
                {
                    return Array.Empty<string>();
                }
                return caps.SupportedCommands;
            }
        }

        public TranscodingInfo TranscodingInfo { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
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

        public bool SupportsRemoteControl
        {
            get
            {
                var caps = Capabilities;
                if (caps == null || !caps.SupportsMediaControl)
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
            _logger.Debug("Creating new {0}", newController.GetType().Name);
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
                if (userId.Equals(additionalUser.UserId))
                {
                    return true;
                }
            }
            return false;
        }

        private readonly object _progressLock = new object();
        private ITimer _progressTimer;
        private PlaybackProgressInfo _lastProgressInfo;

        public void StartAutomaticProgress(ITimerFactory timerFactory, PlaybackProgressInfo progressInfo)
        {
            if (_disposed)
            {
                return;
            }

            lock (_progressLock)
            {
                _lastProgressInfo = progressInfo;

                if (_progressTimer == null)
                {
                    _progressTimer = timerFactory.Create(OnProgressTimerCallback, null, 1000, 1000);
                }
                else
                {
                    _progressTimer.Change(1000, 1000);
                }
            }
        }

        // 1 second
        private const long ProgressIncrement = 10000000;

        private async void OnProgressTimerCallback(object state)
        {
            if (_disposed)
            {
                return;
            }

            var progressInfo = _lastProgressInfo;
            if (progressInfo == null)
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
            long? runtimeTicks = item == null ? null : item.RunTimeTicks;

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
                _logger.ErrorException("Error reporting playback progress", ex);
            }
        }

        public void StopAutomaticProgress()
        {
            lock (_progressLock)
            {
                if (_progressTimer != null)
                {
                    _progressTimer.Dispose();
                    _progressTimer = null;
                }
                _lastProgressInfo = null;
            }
        }

        private bool _disposed = false;

        public void Dispose()
        {
            _disposed = true;

            StopAutomaticProgress();

            var controllers = SessionControllers.ToList();
            SessionControllers = new ISessionController[] { };

            foreach (var controller in controllers)
            {
                var disposable = controller as IDisposable;

                if (disposable != null)
                {
                    _logger.Debug("Disposing session controller {0}", disposable.GetType().Name);

                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error disposing session controller", ex);
                    }
                }
            }

            _sessionManager = null;
        }
    }
}
