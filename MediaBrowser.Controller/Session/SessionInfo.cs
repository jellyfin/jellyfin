using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Class SessionInfo
    /// </summary>
    public class SessionInfo
    {
        public SessionInfo()
        {
            QueueableMediaTypes = new List<string>();
            PlayableMediaTypes = new List<string>
            {
                MediaType.Audio,
                MediaType.Book,
                MediaType.Game,
                MediaType.Photo,
                MediaType.Video
            };

            AdditionalUsers = new List<SessionUserInfo>();
            SupportedCommands = new List<string>();
            PlayState = new PlayerStateInfo();
        }

        public PlayerStateInfo PlayState { get; set; }
        
        public List<SessionUserInfo> AdditionalUsers { get; set; }

        /// <summary>
        /// Gets or sets the remote end point.
        /// </summary>
        /// <value>The remote end point.</value>
        public string RemoteEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the queueable media types.
        /// </summary>
        /// <value>The queueable media types.</value>
        public List<string> QueueableMediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the playable media types.
        /// </summary>
        /// <value>The playable media types.</value>
        public List<string> PlayableMediaTypes { get; set; }

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
        /// Gets or sets the supported commands.
        /// </summary>
        /// <value>The supported commands.</value>
        public List<string> SupportedCommands { get; set; }

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

        /// <summary>
        /// Gets a value indicating whether [supports remote control].
        /// </summary>
        /// <value><c>true</c> if [supports remote control]; otherwise, <c>false</c>.</value>
        public bool SupportsRemoteControl
        {
            get
            {
                if (SessionController != null)
                {
                    return SessionController.SupportsMediaRemoteControl;
                }

                return false;
            }
        }

        public bool ContainsUser(Guid userId)
        {
            return (UserId ?? Guid.Empty) == UserId || AdditionalUsers.Any(i => userId == new Guid(i.UserId));
        }
    }
}
