using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class SessionInfo
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

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
        /// Gets or sets the now playing item.
        /// </summary>
        /// <value>The now playing item.</value>
        public BaseItemInfo NowPlayingItem { get; set; }

        /// <summary>
        /// Gets or sets the now playing position ticks.
        /// </summary>
        /// <value>The now playing position ticks.</value>
        public long? NowPlayingPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        public string DeviceId { get; set; }
    }
}
