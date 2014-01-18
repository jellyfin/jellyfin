using System.ComponentModel;
using System.Diagnostics;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ChannelInfoDto
    /// </summary>
    [DebuggerDisplay("Name = {Name}, Number = {Number}")]
    public class ChannelInfoDto : INotifyPropertyChanged, IItemDto
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the external identifier.
        /// </summary>
        /// <value>The external identifier.</value>
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the image tags.
        /// </summary>
        /// <value>The image tags.</value>
        public Dictionary<ImageType, Guid> ImageTags { get; set; }

        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>The number.</value>
        public string Number { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        public string MediaType { get; set; }

        /// <summary>
        /// Gets or sets the user data.
        /// </summary>
        /// <value>The user data.</value>
        public UserItemDataDto UserData { get; set; }

        /// <summary>
        /// Gets or sets the now playing program.
        /// </summary>
        /// <value>The now playing program.</value>
        public ProgramInfoDto CurrentProgram { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio, after image enhancements.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        public double? PrimaryImageAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio, before image enhancements.
        /// </summary>
        /// <value>The original primary image aspect ratio.</value>
        public double? OriginalPrimaryImageAspectRatio { get; set; }

        public ChannelInfoDto()
        {
            ImageTags = new Dictionary<ImageType, Guid>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
