using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.LiveTv
{
    [DebuggerDisplay("Name = {Name}")]
    public class SeriesTimerInfoDto : BaseTimerInfoDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether [record any time].
        /// </summary>
        /// <value><c>true</c> if [record any time]; otherwise, <c>false</c>.</value>
        public bool RecordAnyTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [record any channel].
        /// </summary>
        /// <value><c>true</c> if [record any channel]; otherwise, <c>false</c>.</value>
        public bool RecordAnyChannel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [record new only].
        /// </summary>
        /// <value><c>true</c> if [record new only]; otherwise, <c>false</c>.</value>
        public bool RecordNewOnly { get; set; }

        /// <summary>
        /// Gets or sets the days.
        /// </summary>
        /// <value>The days.</value>
        public List<DayOfWeek> Days { get; set; }

        /// <summary>
        /// Gets or sets the day pattern.
        /// </summary>
        /// <value>The day pattern.</value>
        public DayPattern? DayPattern { get; set; }

        /// <summary>
        /// Gets or sets the image tags.
        /// </summary>
        /// <value>The image tags.</value>
        public Dictionary<ImageType, string> ImageTags { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has primary image.
        /// </summary>
        /// <value><c>true</c> if this instance has primary image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasPrimaryImage
        {
            get { return ImageTags != null && ImageTags.ContainsKey(ImageType.Primary); }
        }

        public SeriesTimerInfoDto()
        {
            ImageTags = new Dictionary<ImageType, string>();
            Days = new List<DayOfWeek>();
        }
    }
}
