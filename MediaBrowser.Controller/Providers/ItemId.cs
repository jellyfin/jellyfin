using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class ItemId : IHasProviderIds
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the metadata language.
        /// </summary>
        /// <value>The metadata language.</value>
        public string MetadataLanguage { get; set; }
        /// <summary>
        /// Gets or sets the metadata country code.
        /// </summary>
        /// <value>The metadata country code.</value>
        public string MetadataCountryCode { get; set; }
        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        public Dictionary<string, string> ProviderIds { get; set; }
        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        public int? Year { get; set; }
        public int? IndexNumber { get; set; }
        public int? ParentIndexNumber { get; set; }

        public ItemId()
        {
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class AlbumId : ItemId
    {
        /// <summary>
        /// Gets or sets the album artist.
        /// </summary>
        /// <value>The album artist.</value>
        public string AlbumArtist { get; set; }

        /// <summary>
        /// Gets or sets the artist provider ids.
        /// </summary>
        /// <value>The artist provider ids.</value>
        public Dictionary<string, string> ArtistProviderIds { get; set; }

        public AlbumId()
        {
            ArtistProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class GameId : ItemId
    {
        /// <summary>
        /// Gets or sets the game system.
        /// </summary>
        /// <value>The game system.</value>
        public string GameSystem { get; set; }
    }

    public class GameSystemId : ItemId
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }
    }

    public class EpisodeId : ItemId
    {
        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public int? IndexNumberEnd { get; set; }

        public EpisodeId()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
