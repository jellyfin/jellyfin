#pragma warning disable CS1591

using System;
using Jellyfin.Server.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// Class ChapterInfo.
    /// </summary>
    [PrimaryKey(nameof(ItemId), nameof(ChapterIndex))]
    public class ChapterInfo : ILibraryModel
    {
        /// <summary>
        /// Gets or sets the BaseItem Id.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the Chapter Index.
        /// </summary>
        public int ChapterIndex { get; set; }

        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        public long StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the image path.
        /// </summary>
        /// <value>The image path.</value>
        public string? ImagePath { get; set; }

        public DateTime ImageDateModified { get; set; }
    }
}
