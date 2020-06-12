#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.MediaInfo
{
    public class MediaInfo : MediaSourceInfo, IHasProviderIds
    {
        public ChapterInfo[] Chapters { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        public string Album { get; set; }

        /// <summary>
        /// Gets or sets the artists.
        /// </summary>
        /// <value>The artists.</value>
        public string[] Artists { get; set; }

        /// <summary>
        /// Gets or sets the album artists.
        /// </summary>
        /// <value>The album artists.</value>
        public string[] AlbumArtists { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        public string[] Studios { get; set; }
        public string[] Genres { get; set; }
        public string ShowName { get; set; }
        public int? IndexNumber { get; set; }
        public int? ParentIndexNumber { get; set; }
        public int? ProductionYear { get; set; }
        public DateTime? PremiereDate { get; set; }
        public BaseItemPerson[] People { get; set; }
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

        public MediaInfo()
        {
            Chapters = Array.Empty<ChapterInfo>();
            Artists = Array.Empty<string>();
            AlbumArtists = Array.Empty<string>();
            Studios = Array.Empty<string>();
            Genres = Array.Empty<string>();
            People = Array.Empty<BaseItemPerson>();
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
