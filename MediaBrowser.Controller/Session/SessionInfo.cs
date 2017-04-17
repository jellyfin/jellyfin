using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Threading;

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

            AdditionalUsers = new List<SessionUserInfo>();
            PlayState = new PlayerStateInfo();
        }

        public PlayerStateInfo PlayState { get; set; }

        public List<SessionUserInfo> AdditionalUsers { get; set; }

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
        public List<string> PlayableMediaTypes
        {
            get
            {
                if (Capabilities == null)
                {
                    return new List<string>();
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
        public Guid? UserId { get; set; }

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
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the name of the now viewing item.
        /// </summary>
        /// <value>The name of the now viewing item.</value>
        public BaseItemInfo NowViewingItem { get; set; }

        /// <summary>
        /// Gets or sets the now playing item.
        /// </summary>
        /// <value>The now playing item.</value>
        public BaseItemInfo NowPlayingItem { get; set; }

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
        public ISessionController SessionController { get; set; }

        /// <summary>
        /// Gets or sets the application icon URL.
        /// </summary>
        /// <value>The application icon URL.</value>
        public string AppIconUrl { get; set; }

        /// <summary>
        /// Gets or sets the supported commands.
        /// </summary>
        /// <value>The supported commands.</value>
        public List<string> SupportedCommands
        {
            get
            {
                if (Capabilities == null)
                {
                    return new List<string>();
                }
                return Capabilities.SupportedCommands;
            }
        }

        public TranscodingInfo TranscodingInfo { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive
        {
            get
            {
                if (SessionController != null)
                {
                    return SessionController.IsSessionActive;
                }

                return (DateTime.UtcNow - LastActivityDate).TotalMinutes <= 10;
            }
        }

        public bool SupportsMediaControl
        {
            get
            {
                if (Capabilities == null || !Capabilities.SupportsMediaControl)
                {
                    return false;
                }

                if (SessionController != null)
                {
                    return SessionController.SupportsMediaControl;
                }

                return false;
            }
        }

        public bool ContainsUser(string userId)
        {
            return ContainsUser(new Guid(userId));
        }

        public bool ContainsUser(Guid userId)
        {
            return (UserId ?? Guid.Empty) == userId || AdditionalUsers.Any(i => userId == new Guid(i.UserId));
        }

        private readonly object _progressLock = new object();
        private ITimer _progressTimer;
        private PlaybackProgressInfo _lastProgressInfo;

        public void StartAutomaticProgress(ITimerFactory timerFactory, PlaybackProgressInfo progressInfo)
        {
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

        public void Dispose()
        {
            StopAutomaticProgress();
            _sessionManager = null;
        }
    }
}
