using System;
using Jellyfin.Database.Implementations.Enums;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Represents a user list.
    /// </summary>
    public class UserListDto
    {
        /// <summary>
        /// Gets or sets the list identifier.
        /// </summary>
        /// <value>The list identifier.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the list name.
        /// </summary>
        /// <value>The list name.</value>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the list kind.
        /// </summary>
        /// <value>The list kind.</value>
        public UserListKind Kind { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the user's default list.
        /// </summary>
        /// <value><c>true</c> if this is the user's default list; otherwise, <c>false</c>.</value>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether watched items are automatically removed.
        /// </summary>
        /// <value><c>true</c> if watched items are automatically removed; otherwise, <c>false</c>.</value>
        public bool AutoRemoveWatched { get; set; }

        /// <summary>
        /// Gets or sets the list sort index.
        /// </summary>
        /// <value>The list sort index.</value>
        public int SortIndex { get; set; }

        /// <summary>
        /// Gets or sets the date the list was created.
        /// </summary>
        /// <value>The date the list was created.</value>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date the list was last modified.
        /// </summary>
        /// <value>The date the list was last modified.</value>
        public DateTime DateModified { get; set; }
    }
}
