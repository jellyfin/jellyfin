#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Library
{
    public class UserViewQuery
    {
        public UserViewQuery()
        {
            IncludeExternalContent = true;
            PresetViews = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId { get; set; }

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

        public string[] PresetViews { get; set; }
    }
}
