using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace MediaBrowser.Model.Session
{
    [DebuggerDisplay("Client = {Client}, Username = {UserName}")]
    public class SessionInfoDto
    {
        /// <summary>
        /// Gets or sets the supported commands.
        /// </summary>
        /// <value>The supported commands.</value>
        public List<string> SupportedCommands { get; set; }

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
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user primary image tag.
        /// </summary>
        /// <value>The user primary image tag.</value>
        public string UserPrimaryImageTag { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>The name of the user.</value>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the additional users present.
        /// </summary>
        /// <value>The additional users present.</value>
        public List<SessionUserInfo> AdditionalUsers { get; set; }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        /// <value>The application version.</value>
        public string ApplicationVersion { get; set; }
        
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
        /// Gets or sets the now viewing item.
        /// </summary>
        /// <value>The now viewing item.</value>
        public BaseItemInfo NowViewingItem { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName { get; set; }
        
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
        /// Gets or sets the application icon URL.
        /// </summary>
        /// <value>The application icon URL.</value>
        public string AppIconUrl { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether [supports remote control].
        /// </summary>
        /// <value><c>true</c> if [supports remote control]; otherwise, <c>false</c>.</value>
        public bool SupportsRemoteControl { get; set; }

        public PlayerStateInfo PlayState { get; set; }

        public TranscodingInfo TranscodingInfo { get; set; }
        
        public SessionInfoDto()
        {
            AdditionalUsers = new List<SessionUserInfo>();

            PlayableMediaTypes = new List<string>();
            QueueableMediaTypes = new List<string>();
            SupportedCommands = new List<string>();
        }
    }
}
