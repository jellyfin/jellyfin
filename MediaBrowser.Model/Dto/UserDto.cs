using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Users;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class UserDto
    /// </summary>
    [DebuggerDisplay("Name = {Name}, ID = {Id}, HasPassword = {HasPassword}")]
    public class UserDto : IItemDto, IHasServerId
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
        /// Gets or sets the name of the server.
        /// This is not used by the server and is for client-side usage only.
        /// </summary>
        /// <value>The name of the server.</value>
        public string ServerName { get; set; }

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
        /// Gets or sets the offline password.
        /// </summary>
        /// <value>The offline password.</value>
        public string OfflinePassword { get; set; }

        /// <summary>
        /// Gets or sets the offline password salt.
        /// </summary>
        /// <value>The offline password salt.</value>
        public string OfflinePasswordSalt { get; set; }
        
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

        /// <summary>
        /// Gets or sets a value indicating whether this instance has configured password.
        /// </summary>
        /// <value><c>true</c> if this instance has configured password; otherwise, <c>false</c>.</value>
        public bool HasConfiguredPassword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has configured easy password.
        /// </summary>
        /// <value><c>true</c> if this instance has configured easy password; otherwise, <c>false</c>.</value>
        public bool HasConfiguredEasyPassword { get; set; }
        
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
        /// Gets or sets the policy.
        /// </summary>
        /// <value>The policy.</value>
        public UserPolicy Policy { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        public double? PrimaryImageAspectRatio { get; set; }

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
            Policy = new UserPolicy();
        }

        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
