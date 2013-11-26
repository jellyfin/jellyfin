using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    public class ProgramInfoDto
    {
        /// <summary>
        /// Id of the program.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the external identifier.
        /// </summary>
        /// <value>The external identifier.</value>
        public string ExternalId { get; set; }
        
        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        public float? CommunityRating { get; set; }
        
        /// <summary>
        /// Gets or sets the recording identifier.
        /// </summary>
        /// <value>The recording identifier.</value>
        public string RecordingId { get; set; }

        /// <summary>
        /// Gets or sets the official rating.
        /// </summary>
        /// <value>The official rating.</value>
        public string OfficialRating { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        public string ServiceName { get; set; }

        /// <summary>
        /// Name of the program
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the progam.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The start date of the program, in UTC.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end date of the program, in UTC.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Genre of the program.
        /// </summary>
        public List<string> Genres { get; set; }

        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        public ProgramVideoQuality Quality { get; set; }

        /// <summary>
        /// Gets or sets the audio.
        /// </summary>
        /// <value>The audio.</value>
        public ProgramAudio Audio { get; set; }
        
        /// <summary>
        /// Gets or sets the original air date.
        /// </summary>
        /// <value>The original air date.</value>
        public DateTime? OriginalAirDate { get; set; }
        
        public ProgramInfoDto()
        {
            Genres = new List<string>();
        }
    }

    public enum ProgramVideoQuality
    {
        StandardDefinition,
        HighDefinition
    }

    public enum ProgramAudio
    {
        Stereo
    }
}