using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    public class RecordingInfoDto
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
        /// Quality of the Recording.
        /// </summary>
        public string Quality { get; set; } // TODO: Enum for quality?? Difference NextPvr,Argus,...

        /// <summary>
        /// Recurring recording?
        /// </summary>
        public bool Recurring { get; set; }

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