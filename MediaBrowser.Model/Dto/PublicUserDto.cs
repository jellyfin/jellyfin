#nullable disable
using System;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class PublicUserDto.
    /// </summary>
    public class PublicUserDto : IItemDto, IHasServerId
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PublicUserDto"/> class.
        /// </summary>
        public PublicUserDto()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicUserDto"/> class from a <see cref="UserDto"/> object.
        /// </summary>
        /// <param name="sourceUser">The <see cref="UserDto"/> object from which to construct this <see cref="PublicUserDto"/>.</param>
        public PublicUserDto(UserDto sourceUser)
        {
            this.Name = sourceUser.Name;
            this.ServerId = sourceUser.ServerId;
            this.ServerName = sourceUser.ServerName;
            this.Id = sourceUser.Id;
            this.PrimaryImageTag = sourceUser.PrimaryImageTag;
            this.HasPassword = sourceUser.HasPassword;
            this.PrimaryImageAspectRatio = sourceUser.PrimaryImageAspectRatio;
        }

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
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }

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
        /// Gets or sets the primary image aspect ratio.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        public double? PrimaryImageAspectRatio { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name ?? base.ToString();
        }
    }
}
