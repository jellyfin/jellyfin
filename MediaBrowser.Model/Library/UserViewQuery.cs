#pragma warning disable CS1591

using System;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Library
{
    public class UserViewQuery
    {
        public UserViewQuery()
        {
            IncludeExternalContent = true;
            PresetViews = Array.Empty<CollectionType?>();
        }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public required User User { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include external content].
        /// </summary>
        /// <value><c>true</c> if [include external content]; otherwise, <c>false</c>.</value>
        public bool IncludeExternalContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include hidden].
        /// </summary>
        /// <value><c>true</c> if [include hidden]; otherwise, <c>false</c>.</value>
        public bool IncludeHidden { get; set; }

        public CollectionType?[] PresetViews { get; set; }
    }
}
