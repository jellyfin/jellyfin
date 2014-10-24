using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Extensions;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class UserDto
    /// </summary>
    [DebuggerDisplay("Name = {Name}, ID = {Id}, HasPassword = {HasPassword}")]
    public class UserDto : IHasPropertyChangedEvent, IItemDto, IHasServerId
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        /// <value>The server identifier.</value>
        public string ServerId { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the connect user.
        /// </summary>
        /// <value>The name of the connect user.</value>
        public string ConnectUserName { get; set; }
        /// <summary>
        /// Gets or sets the connect user identifier.
        /// </summary>
        /// <value>The connect user identifier.</value>
        public string ConnectUserId { get; set; }
        /// <summary>
        /// Gets or sets the type of the connect link.
        /// </summary>
        /// <value>The type of the connect link.</value>
        public UserLinkType? ConnectLinkType { get; set; }
        
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the primary image tag.
        /// </summary>
        /// <value>The primary image tag.</value>
        public string PrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has password.
        /// </summary>
        /// <value><c>true</c> if this instance has password; otherwise, <c>false</c>.</value>
        public bool HasPassword { get; set; }

        public bool HasConfiguredPassword { get; set; }
        
        /// <summary>
        /// Gets or sets the last login date.
        /// </summary>
        /// <value>The last login date.</value>
        public DateTime? LastLoginDate { get; set; }

        /// <summary>
        /// Gets or sets the last activity date.
        /// </summary>
        /// <value>The last activity date.</value>
        public DateTime? LastActivityDate { get; set; }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public UserConfiguration Configuration { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        public double? PrimaryImageAspectRatio { get; set; }

        /// <summary>
        /// Gets or sets the original primary image aspect ratio.
        /// </summary>
        /// <value>The original primary image aspect ratio.</value>
        public double? OriginalPrimaryImageAspectRatio { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether this instance has primary image.
        /// </summary>
        /// <value><c>true</c> if this instance has primary image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasPrimaryImage
        {
            get { return PrimaryImageTag != null; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserDto"/> class.
        /// </summary>
        public UserDto()
        {
            Configuration = new UserConfiguration();
        }

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
