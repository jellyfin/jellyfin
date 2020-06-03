#nullable disable
using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Defines the display preferences for any item that supports them (usually Folders).
    /// </summary>
    public class DisplayPreferences
    {
        /// <summary>
        /// The image scale.
        /// </summary>
        private const double ImageScale = .9;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferences" /> class.
        /// </summary>
        public DisplayPreferences()
        {
            RememberIndexing = false;
            PrimaryImageHeight = 250;
            PrimaryImageWidth = 250;
            ShowBackdrop = true;
            CustomPrefs = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the view.
        /// </summary>
        /// <value>The type of the view.</value>
        public string ViewType { get; set; }

        /// <summary>
        /// Gets or sets the sort by.
        /// </summary>
        /// <value>The sort by.</value>
        public string SortBy { get; set; }

        /// <summary>
        /// Gets or sets the index by.
        /// </summary>
        /// <value>The index by.</value>
        public string IndexBy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [remember indexing].
        /// </summary>
        /// <value><c>true</c> if [remember indexing]; otherwise, <c>false</c>.</value>
        public bool RememberIndexing { get; set; }

        /// <summary>
        /// Gets or sets the height of the primary image.
        /// </summary>
        /// <value>The height of the primary image.</value>
        public int PrimaryImageHeight { get; set; }

        /// <summary>
        /// Gets or sets the width of the primary image.
        /// </summary>
        /// <value>The width of the primary image.</value>
        public int PrimaryImageWidth { get; set; }

        /// <summary>
        /// Gets or sets the custom prefs.
        /// </summary>
        /// <value>The custom prefs.</value>
        public Dictionary<string, string> CustomPrefs { get; set; }

        /// <summary>
        /// Gets or sets the scroll direction.
        /// </summary>
        /// <value>The scroll direction.</value>
        public ScrollDirection ScrollDirection { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show backdrops on this item.
        /// </summary>
        /// <value><c>true</c> if showing backdrops; otherwise, <c>false</c>.</value>
        public bool ShowBackdrop { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [remember sorting].
        /// </summary>
        /// <value><c>true</c> if [remember sorting]; otherwise, <c>false</c>.</value>
        public bool RememberSorting { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>The sort order.</value>
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show sidebar].
        /// </summary>
        /// <value><c>true</c> if [show sidebar]; otherwise, <c>false</c>.</value>
        public bool ShowSidebar { get; set; }

        /// <summary>
        /// Gets or sets the client
        /// </summary>
        public string Client { get; set; }
    }
}
