using System;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class PublicUserDto. Its goal is to show only public information about a user
    /// </summary>
    public class PublicUserDto : IItemDto
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

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
        /// Note that in this case this method should not be here, but it is necessary when changeing password at the
        /// first login.
        /// </summary>
        /// <value><c>true</c> if this instance has configured password; otherwise, <c>false</c>.</value>
        public bool HasConfiguredPassword { get; set; }

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