using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is a stub class containing only basic information about an item
    /// </summary>
    [DebuggerDisplay("Name = {Name}, ID = {Id}, Type = {Type}")]
    public class BaseItemInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        public string MediaType { get; set; }
        
        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the primary image tag.
        /// </summary>
        /// <value>The primary image tag.</value>
        public Guid? PrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the primary image item identifier.
        /// </summary>
        /// <value>The primary image item identifier.</value>
        public string PrimaryImageItemId { get; set; }
        
        /// <summary>
        /// Gets or sets the thumb image tag.
        /// </summary>
        /// <value>The thumb image tag.</value>
        public Guid? ThumbImageTag { get; set; }

        /// <summary>
        /// Gets or sets the thumb item identifier.
        /// </summary>
        /// <value>The thumb item identifier.</value>
        public string ThumbItemId { get; set; }

        /// <summary>
        /// Gets or sets the thumb image tag.
        /// </summary>
        /// <value>The thumb image tag.</value>
        public Guid? BackdropImageTag { get; set; }

        /// <summary>
        /// Gets or sets the thumb item identifier.
        /// </summary>
        /// <value>The thumb item identifier.</value>
        public string BackdropItemId { get; set; }

        /// <summary>
        /// Gets or sets the premiere date.
        /// </summary>
        /// <value>The premiere date.</value>
        public DateTime? PremiereDate { get; set; }

        /// <summary>
        /// Gets or sets the production year.
        /// </summary>
        /// <value>The production year.</value>
        public int? ProductionYear { get; set; }

        /// <summary>
        /// Gets or sets the index number.
        /// </summary>
        /// <value>The index number.</value>
        public int? IndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the index number end.
        /// </summary>
        /// <value>The index number end.</value>
        public int? IndexNumberEnd { get; set; }

        /// <summary>
        /// Gets or sets the parent index number.
        /// </summary>
        /// <value>The parent index number.</value>
        public int? ParentIndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        public string SeriesName { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string Album { get; set; }

        /// <summary>
        /// Gets or sets the artists.
        /// </summary>
        /// <value>The artists.</value>
        public List<string> Artists { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether this instance has primary image.
        /// </summary>
        /// <value><c>true</c> if this instance has primary image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasPrimaryImage
        {
            get { return PrimaryImageTag.HasValue; }
        }

        public BaseItemInfo()
        {
            Artists = new List<string>();
        }
    }
}
