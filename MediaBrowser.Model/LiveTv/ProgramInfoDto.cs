using System.ComponentModel;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.LiveTv
{
    public class ProgramInfoDto : INotifyPropertyChanged
    {
        /// <summary>
        /// Id of the program.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the timer identifier.
        /// </summary>
        /// <value>The timer identifier.</value>
        public string TimerId { get; set; }

        /// <summary>
        /// Gets or sets the series timer identifier.
        /// </summary>
        /// <value>The series timer identifier.</value>
        public string SeriesTimerId { get; set; }
        
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
        /// Gets or sets the name of the channel.
        /// </summary>
        /// <value>The name of the channel.</value>
        public string ChannelName { get; set; }
        
        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        public float? CommunityRating { get; set; }
        
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
        /// Overview of the recording.
        /// </summary>
        public string Overview { get; set; }

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
        /// Gets or sets a value indicating whether this instance is hd.
        /// </summary>
        /// <value><c>true</c> if this instance is hd; otherwise, <c>false</c>.</value>
        public bool? IsHD { get; set; }

        /// <summary>
        /// Gets or sets the audio.
        /// </summary>
        /// <value>The audio.</value>
        public ProgramAudio? Audio { get; set; }
        
        /// <summary>
        /// Gets or sets the original air date.
        /// </summary>
        /// <value>The original air date.</value>
        public DateTime? OriginalAirDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is repeat.
        /// </summary>
        /// <value><c>true</c> if this instance is repeat; otherwise, <c>false</c>.</value>
        public bool IsRepeat { get; set; }

        /// <summary>
        /// Gets or sets the episode title.
        /// </summary>
        /// <value>The episode title.</value>
        public string EpisodeTitle { get; set; }

        /// <summary>
        /// Gets or sets the image tags.
        /// </summary>
        /// <value>The image tags.</value>
        public Dictionary<ImageType, Guid> ImageTags { get; set; }
        
        /// <summary>
        /// Gets or sets the user data.
        /// </summary>
        /// <value>The user data.</value>
        public UserItemDataDto UserData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is movie.
        /// </summary>
        /// <value><c>true</c> if this instance is movie; otherwise, <c>false</c>.</value>
        public bool IsMovie { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>true</c> if this instance is sports; otherwise, <c>false</c>.</value>
        public bool IsSports { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is series.
        /// </summary>
        /// <value><c>true</c> if this instance is series; otherwise, <c>false</c>.</value>
        public bool IsSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is live.
        /// </summary>
        /// <value><c>true</c> if this instance is live; otherwise, <c>false</c>.</value>
        public bool IsLive { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is news.
        /// </summary>
        /// <value><c>true</c> if this instance is news; otherwise, <c>false</c>.</value>
        public bool IsNews { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>true</c> if this instance is kids; otherwise, <c>false</c>.</value>
        public bool IsKids { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is premiere.
        /// </summary>
        /// <value><c>true</c> if this instance is premiere; otherwise, <c>false</c>.</value>
        public bool IsPremiere { get; set; }

        public ProgramInfoDto()
        {
            Genres = new List<string>();
            ImageTags = new Dictionary<ImageType, Guid>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public enum ProgramAudio
    {
        Mono,
        Stereo,
        Dolby,
        DolbyDigital,
        Thx
    }
}