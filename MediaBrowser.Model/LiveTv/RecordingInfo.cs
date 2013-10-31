using System;

namespace MediaBrowser.Model.LiveTv
{
    public class RecordingInfo
    {
        public string ChannelId { get; set; }

        public string ChannelName { get; set; }

        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// The start date of the recording, in UTC
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end date of the recording, in UTC
        /// </summary>
        public DateTime EndDate { get; set; }
    }
}
