using System;

namespace MediaBrowser.Model.LiveTv
{
    public class TimerInfoDto
    {
        /// <summary>
        /// Id of the recording.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the external identifier.
        /// </summary>
        /// <value>The external identifier.</value>
        public string ExternalId { get; set; }

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
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public RecordingStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the series timer identifier.
        /// </summary>
        /// <value>The series timer identifier.</value>
        public string SeriesTimerId { get; set; }

        /// <summary>
        /// Gets or sets the requested pre padding seconds.
        /// </summary>
        /// <value>The requested pre padding seconds.</value>
        public int RequestedPrePaddingSeconds { get; set; }

        /// <summary>
        /// Gets or sets the requested post padding seconds.
        /// </summary>
        /// <value>The requested post padding seconds.</value>
        public int RequestedPostPaddingSeconds { get; set; }

        /// <summary>
        /// Gets or sets the required pre padding seconds.
        /// </summary>
        /// <value>The required pre padding seconds.</value>
        public int RequiredPrePaddingSeconds { get; set; }

        /// <summary>
        /// Gets or sets the required post padding seconds.
        /// </summary>
        /// <value>The required post padding seconds.</value>
        public int RequiredPostPaddingSeconds { get; set; }

        /// <summary>
        /// Gets or sets the duration ms.
        /// </summary>
        /// <value>The duration ms.</value>
        public int DurationMs { get; set; }
    }
}
