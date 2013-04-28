using System;

namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class ThemeSongsResult
    /// </summary>
    public class ThemeSongsResult : ItemsResult
    {
        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        /// <value>The owner id.</value>
        public string OwnerId { get; set; }
    }

    public class ThemeVideosResult : ItemsResult
    {
        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        /// <value>The owner id.</value>
        public string OwnerId { get; set; }
    }
}
