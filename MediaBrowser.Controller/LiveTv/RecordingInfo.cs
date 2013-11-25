using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.LiveTv
{
    public class RecordingInfo
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
        /// ChannelName of the recording.
        /// </summary>
        public string ChannelName { get; set; }

        /// <summary>
        /// Gets or sets the program identifier.
        /// </summary>
        /// <value>The program identifier.</value>
        public string ProgramId { get; set; }
        
        /// <summary>
        /// Name of the recording.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the recording.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The start date of the recording, in UTC.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end date of the recording, in UTC.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Status of the recording.
        /// </summary>
        public string Status { get; set; } //TODO: Enum for status?? Difference NextPvr,Argus,...

        /// <summary>
        /// Gets or sets a value indicating whether this instance is recurring.
        /// </summary>
        /// <value><c>true</c> if this instance is recurring; otherwise, <c>false</c>.</value>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Parent recurring.
        /// </summary>
        public string RecurringParent { get; set; }

        /// <summary>
        /// Start date for the recurring, in UTC.
        /// </summary>
        public DateTime RecurrringStartDate { get; set; }

        /// <summary>
        /// End date for the recurring, in UTC
        /// </summary>
        public DateTime RecurringEndDate { get; set; }

        /// <summary>
        /// When do we need the recording?
        /// </summary>
        public List<string> DayMask { get; set; }
    }
}
