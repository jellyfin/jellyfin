#pragma warning disable CS1591

using System.ComponentModel;
using Jellyfin.Database.Implementations.Enums;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Options for a specific home section.
    /// </summary>
    public class HomeSectionOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HomeSectionOptions"/> class.
        /// </summary>
        public HomeSectionOptions()
        {
            Name = string.Empty;
        }

        /// <summary>
        /// Gets or sets the name of the section.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the section.
        /// </summary>
        public HomeSectionType SectionType { get; set; }

        /// <summary>
        /// Gets or sets the priority/order of this section (lower numbers appear first).
        /// </summary>
        [DefaultValue(0)]
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum number of items to display in the section.
        /// </summary>
        [DefaultValue(10)]
        public int MaxItems { get; set; } = 10;

        /// <summary>
        /// Gets or sets the sort order for items in this section.
        /// </summary>
        [DefaultValue(SortOrder.Ascending)]
        public SortOrder SortOrder { get; set; } = SortOrder.Ascending;

        /// <summary>
        /// Gets or sets how items should be sorted in this section.
        /// </summary>
        [DefaultValue(SortOrder.Ascending)]
        public SortOrder SortBy { get; set; } = SortOrder.Ascending;
    }
}
