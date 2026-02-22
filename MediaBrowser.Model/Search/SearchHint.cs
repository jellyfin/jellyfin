using System;
using System.Collections.Generic;
using System.ComponentModel;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Search
{
    /// <summary>
    /// Class SearchHintResult.
    /// </summary>
    public class SearchHint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchHint" /> class.
        /// </summary>
        public SearchHint()
        {
            Name = string.Empty;
            MatchedTerm = string.Empty;
            MediaType = Jellyfin.Data.Enums.MediaType.Unknown;
            Artists = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        [Obsolete("Use Id instead")]
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the matched term.
        /// </summary>
        /// <value>The matched term.</value>
        public string? MatchedTerm { get; set; }

        /// <summary>
        /// Gets or sets the index number.
        /// </summary>
        /// <value>The index number.</value>
        public int? IndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the production year.
        /// </summary>
        /// <value>The production year.</value>
        public int? ProductionYear { get; set; }

        /// <summary>
        /// Gets or sets the parent index number.
        /// </summary>
        /// <value>The parent index number.</value>
        public int? ParentIndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the image tag.
        /// </summary>
        /// <value>The image tag.</value>
        public string? PrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the thumb image tag.
        /// </summary>
        /// <value>The thumb image tag.</value>
        public string? ThumbImageTag { get; set; }

        /// <summary>
        /// Gets or sets the thumb image item identifier.
        /// </summary>
        /// <value>The thumb image item identifier.</value>
        public string? ThumbImageItemId { get; set; }

        /// <summary>
        /// Gets or sets the backdrop image tag.
        /// </summary>
        /// <value>The backdrop image tag.</value>
        public string? BackdropImageTag { get; set; }

        /// <summary>
        /// Gets or sets the backdrop image item identifier.
        /// </summary>
        /// <value>The backdrop image item identifier.</value>
        public string? BackdropImageItemId { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public BaseItemKind Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        public bool? IsFolder { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [DefaultValue(MediaType.Unknown)]
        public MediaType MediaType { get; set; }

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the series.
        /// </summary>
        /// <value>The series.</value>
        public string? Series { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string? Album { get; set; }

        /// <summary>
        /// Gets or sets the album id.
        /// </summary>
        /// <value>The album id.</value>
        public Guid? AlbumId { get; set; }

        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        public string? AlbumArtist { get; set; }

        /// <summary>
        /// Gets or sets the artists.
        /// </summary>
        /// <value>The artists.</value>
        public IReadOnlyList<string> Artists { get; set; }

        /// <summary>
        /// Gets or sets the song count.
        /// </summary>
        /// <value>The song count.</value>
        public int? SongCount { get; set; }

        /// <summary>
        /// Gets or sets the episode count.
        /// </summary>
        /// <value>The episode count.</value>
        public int? EpisodeCount { get; set; }

        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public Guid? ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the name of the channel.
        /// </summary>
        /// <value>The name of the channel.</value>
        public string? ChannelName { get; set; }

        /// <summary>
        /// Gets or sets the primary image aspect ratio.
        /// </summary>
        /// <value>The primary image aspect ratio.</value>
        public double? PrimaryImageAspectRatio { get; set; }
    }
}
