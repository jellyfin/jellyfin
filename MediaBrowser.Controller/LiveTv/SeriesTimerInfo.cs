using System;
using System.Collections.Generic;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv
{
    public class SeriesTimerInfo
    {
        /// <summary>
        /// Id of the recording.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// ChannelId of the recording.
        /// </summary>
        public string ChannelId { get; set; }
        
        /// <summary>
        /// Gets or sets the program identifier.
        /// </summary>
        /// <value>The program identifier.</value>
        public string ProgramId { get; set; }
        
        /// <summary>
        /// Name of the recording.
        /// </summary>
        public string Name { get; set; }

        public string ServiceName { get; set; }

        /// <summary>
        /// Description of the recording.
        /// </summary>
        public string Overview { get; set; }

        /// <summary>
        /// The start date of the recording, in UTC.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end date of the recording, in UTC.
        /// </summary>
        public DateTime EndDate { get; set; }

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

        public int KeepUpTo { get; set; }
        public KeepUntil KeepUntil { get; set; }

        public bool SkipEpisodesInLibrary { get; set; }

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
        /// Gets or sets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the pre padding seconds.
        /// </summary>
        /// <value>The pre padding seconds.</value>
        public int PrePaddingSeconds { get; set; }

        /// <summary>
        /// Gets or sets the post padding seconds.
        /// </summary>
        /// <value>The post padding seconds.</value>
        public int PostPaddingSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is pre padding required.
        /// </summary>
        /// <value><c>true</c> if this instance is pre padding required; otherwise, <c>false</c>.</value>
        public bool IsPrePaddingRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is post padding required.
        /// </summary>
        /// <value><c>true</c> if this instance is post padding required; otherwise, <c>false</c>.</value>
        public bool IsPostPaddingRequired { get; set; }

        /// <summary>
        /// Gets or sets the series identifier.
        /// </summary>
        /// <value>The series identifier.</value>
        public string SeriesId { get; set; }
        
        public SeriesTimerInfo()
        {
            Days = new List<DayOfWeek>();
            SkipEpisodesInLibrary = true;
            KeepUntil = KeepUntil.UntilDeleted;
        }
    }
}
