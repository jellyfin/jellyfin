#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class ChapterInfo.
    /// </summary>
    public class ChapterInfo
    {
        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        public long StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the image path.
        /// </summary>
        /// <value>The image path.</value>
        public string ImagePath { get; set; }

        public DateTime ImageDateModified { get; set; }

        public string ImageTag { get; set; }
    }
}
