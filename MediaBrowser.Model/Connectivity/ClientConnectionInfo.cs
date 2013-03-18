using MediaBrowser.Model.Entities;
using ProtoBuf;
using System;

namespace MediaBrowser.Model.Connectivity
{
    /// <summary>
    /// Class ClientConnectionInfo
    /// </summary>
    [ProtoContract]
    public class ClientConnectionInfo
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ProtoMember(1)]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the type of the client.
        /// </summary>
        /// <value>The type of the client.</value>
        [ProtoMember(2)]
        public string Client { get; set; }

        /// <summary>
        /// Gets or sets the last activity date.
        /// </summary>
        /// <value>The last activity date.</value>
        [ProtoMember(3)]
        public DateTime LastActivityDate { get; set; }

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        [ProtoMember(4)]
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the now playing item.
        /// </summary>
        /// <value>The now playing item.</value>
        [ProtoMember(5)]
        public BaseItemInfo NowPlayingItem { get; set; }

        /// <summary>
        /// Gets or sets the now playing position ticks.
        /// </summary>
        /// <value>The now playing position ticks.</value>
        [ProtoMember(6)]
        public long? NowPlayingPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        [ProtoMember(7)]
        public string DeviceId { get; set; }
    }
}
