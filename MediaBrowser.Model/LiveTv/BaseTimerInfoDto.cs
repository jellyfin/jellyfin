using MediaBrowser.Model.Dto;
using System;

namespace MediaBrowser.Model.LiveTv
{
    public class BaseTimerInfoDto : IHasServerId
    {
        /// <summary>
        /// Id of the recording.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        /// <value>The server identifier.</value>
        public string ServerId { get; set; }

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
        /// Gets or sets the external channel identifier.
        /// </summary>
        /// <value>The external channel identifier.</value>
        public string ExternalChannelId { get; set; }

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
        /// Gets or sets the external program identifier.
        /// </summary>
        /// <value>The external program identifier.</value>
        public string ExternalProgramId { get; set; }

        /// <summary>
        /// Name of the recording.
        /// </summary>
        public string Name { get; set; }

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
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        public string ServiceName { get; set; }

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
    }
}