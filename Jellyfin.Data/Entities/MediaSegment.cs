#nullable disable
#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// A moment in time of a media stream (ItemId+StreamIndex) with Type and possible Action applicable between StartTicks/Endticks.
    /// </summary>
    public class MediaSegment
    {
        /// <summary>
        /// Gets or sets the start position in Ticks.
        /// </summary>
        /// <value>The start position.</value>
        public long StartTicks { get; set; }

        /// <summary>
        /// Gets or sets the end position in Ticks.
        /// </summary>
        /// <value>The end position.</value>
        public long EndTicks { get; set; }

        /// <summary>
        /// Gets or sets the Type.
        /// </summary>
        /// <value>The media segment type.</value>
        public MediaSegmentType Type { get; set; }

        /// <summary>
        /// Gets or sets the TypeIndex which relates to the type.
        /// </summary>
        /// <value>The type index.</value>
        public int TypeIndex { get; set; }

        /// <summary>
        /// Gets or sets the associated MediaSourceId.
        /// </summary>
        /// <value>The id.</value>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the associated MediaStreamIndex.
        /// </summary>
        /// <value>The id.</value>
        public int StreamIndex { get; set; }

        /// <summary>
        /// Gets or sets the creator recommended action. Can be overwritten with user defined action.
        /// </summary>
        /// <value>The media segment action.</value>
        public MediaSegmentAction Action { get; set; }

        /// <summary>
        /// Gets or sets a comment.
        /// </summary>
        /// <value>The media segment action.</value>
        public string Comment { get; set; }
    }
}
