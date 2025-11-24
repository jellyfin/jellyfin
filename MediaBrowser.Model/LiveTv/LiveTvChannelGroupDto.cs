#nullable disable

using System;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Represents a logical group of live tv channels.
    /// </summary>
    public class LiveTvChannelGroupDto
    {
        /// <summary>
        /// Gets or sets the unique id of the channel group.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group was created by a user.
        /// </summary>
        public bool? IsUserCreated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the group is hidden.
        /// </summary>
        public bool? IsHidden { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether membership is computed dynamically.
        /// </summary>
        public bool? IsDynamic { get; set; }

        /// <summary>
        /// Gets or sets the number of channels in this group.
        /// </summary>
        public int? ChannelCount { get; set; }
    }
}
