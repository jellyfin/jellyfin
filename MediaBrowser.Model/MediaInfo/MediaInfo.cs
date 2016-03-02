using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.MediaInfo
{
    public class MediaInfo : MediaSourceInfo, IHasProviderIds
    {
        public List<ChapterInfo> Chapters { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string Album { get; set; }
        /// <summary>
        /// Gets or sets the artists.
        /// </summary>
        /// <value>The artists.</value>
        public List<string> Artists { get; set; }
        /// <summary>
        /// Gets or sets the album artists.
        /// </summary>
        /// <value>The album artists.</value>
        public List<string> AlbumArtists { get; set; }
        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        public List<string> Studios { get; set; }
        public List<string> Genres { get; set; }
        public int? IndexNumber { get; set; }
        public int? ParentIndexNumber { get; set; }
        public int? ProductionYear { get; set; }
        public DateTime? PremiereDate { get; set; }
        public List<BaseItemPerson> People { get; set; }
        public Dictionary<string, string> ProviderIds { get; set; }
        /// <summary>
        /// Gets or sets the official rating.
        /// </summary>
        /// <value>The official rating.</value>
        public string OfficialRating { get; set; }
        /// <summary>
        /// Gets or sets the official rating description.
        /// </summary>
        /// <value>The official rating description.</value>
        public string OfficialRatingDescription { get; set; }
        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        public string Overview { get; set; }
        /// <summary>
        /// Gets or sets the short overview.
        /// </summary>
        /// <value>The short overview.</value>
        public string ShortOverview { get; set; }

        public MediaInfo()
        {
            Chapters = new List<ChapterInfo>();
            Artists = new List<string>();
            AlbumArtists = new List<string>();
            Studios = new List<string>();
            Genres = new List<string>();
            People = new List<BaseItemPerson>();
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}