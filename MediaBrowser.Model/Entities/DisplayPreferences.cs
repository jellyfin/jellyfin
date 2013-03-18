using MediaBrowser.Model.Drawing;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Defines the display preferences for any item that supports them (usually Folders)
    /// </summary>
    [ProtoContract]
    public class DisplayPreferences
    {
        /// <summary>
        /// The image scale
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
            CustomPrefs = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ProtoMember(1)]
        public Guid UserId { get; set; }
        /// <summary>
        /// Gets or sets the type of the view.
        /// </summary>
        /// <value>The type of the view.</value>
        [ProtoMember(2)]
        public string ViewType { get; set; }
        /// <summary>
        /// Gets or sets the sort by.
        /// </summary>
        /// <value>The sort by.</value>
        [ProtoMember(4)]
        public string SortBy { get; set; }
        /// <summary>
        /// Gets or sets the index by.
        /// </summary>
        /// <value>The index by.</value>
        [ProtoMember(5)]
        public string IndexBy { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [remember indexing].
        /// </summary>
        /// <value><c>true</c> if [remember indexing]; otherwise, <c>false</c>.</value>
        [ProtoMember(6)]
        public bool RememberIndexing { get; set; }
        /// <summary>
        /// Gets or sets the height of the primary image.
        /// </summary>
        /// <value>The height of the primary image.</value>
        [ProtoMember(7)]
        public int PrimaryImageHeight { get; set; }
        /// <summary>
        /// Gets or sets the width of the primary image.
        /// </summary>
        /// <value>The width of the primary image.</value>
        [ProtoMember(8)]
        public int PrimaryImageWidth { get; set; }
        /// <summary>
        /// Gets or sets the custom prefs.
        /// </summary>
        /// <value>The custom prefs.</value>
        [ProtoMember(9)]
        public Dictionary<string, string> CustomPrefs { get; set; }
        /// <summary>
        /// Gets or sets the scroll direction.
        /// </summary>
        /// <value>The scroll direction.</value>
        [ProtoMember(10)]
        public ScrollDirection ScrollDirection { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [remember sorting].
        /// </summary>
        /// <value><c>true</c> if [remember sorting]; otherwise, <c>false</c>.</value>
        [ProtoMember(11)]
        public bool RememberSorting { get; set; }
        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>The sort order.</value>
        [ProtoMember(12)]
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// Increases the size of the image.
        /// </summary>
        public void IncreaseImageSize()
        {
            var newWidth = PrimaryImageWidth / ImageScale;

            var size = DrawingUtils.Resize(PrimaryImageWidth, PrimaryImageHeight, newWidth);

            PrimaryImageWidth = Convert.ToInt32(size.Width);
            PrimaryImageHeight = Convert.ToInt32(size.Height);
        }

        /// <summary>
        /// Decreases the size of the image.
        /// </summary>
        public void DecreaseImageSize()
        {
            var size = DrawingUtils.Scale(PrimaryImageWidth, PrimaryImageHeight, ImageScale);

            PrimaryImageWidth = Convert.ToInt32(size.Width);
            PrimaryImageHeight = Convert.ToInt32(size.Height);
        }
    }

    /// <summary>
    /// Enum ScrollDirection
    /// </summary>
    public enum ScrollDirection
    {
        /// <summary>
        /// The horizontal
        /// </summary>
        Horizontal,
        /// <summary>
        /// The vertical
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Enum SortOrder
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// The ascending
        /// </summary>
        Ascending,
        /// <summary>
        /// The descending
        /// </summary>
        Descending
    }
}
