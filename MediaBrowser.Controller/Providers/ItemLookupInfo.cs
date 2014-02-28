using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Providers
{
    public class ItemLookupInfo : IHasProviderIds
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

        public ItemLookupInfo()
        {
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public interface IHasLookupInfo<out TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo, new()
    {
        TLookupInfoType GetLookupInfo();
    }

    public class ArtistInfo : ItemLookupInfo
    {
        public List<SongInfo> SongInfos { get; set; }

        public ArtistInfo()
        {
            SongInfos = new List<SongInfo>();
        }
    }

    public class AlbumInfo : ItemLookupInfo
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
        public List<SongInfo> SongInfos { get; set; }

        public AlbumInfo()
        {
            ArtistProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SongInfos = new List<SongInfo>();
        }
    }

    public class GameInfo : ItemLookupInfo
    {
        /// <summary>
        /// Gets or sets the game system.
        /// </summary>
        /// <value>The game system.</value>
        public string GameSystem { get; set; }
    }

    public class GameSystemInfo : ItemLookupInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }
    }

    public class EpisodeInfo : ItemLookupInfo
    {
        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public int? IndexNumberEnd { get; set; }
        public int? AnimeSeriesIndex { get; set; }

        public EpisodeInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class SongInfo : ItemLookupInfo
    {
        public string AlbumArtist { get; set; }
        public string Album { get; set; }
        public List<string> Artists { get; set; }
    }

    public class SeriesInfo : ItemLookupInfo
    {
        public int? AnimeSeriesIndex { get; set; }
    }

    public class PersonLookupInfo : ItemLookupInfo
    {
        
    }

    public class MovieInfo : ItemLookupInfo
    {

    }

    public class BoxSetInfo : ItemLookupInfo
    {

    }

    public class MusicVideoInfo : ItemLookupInfo
    {

    }

    public class TrailerInfo : ItemLookupInfo
    {
        public bool IsLocalTrailer { get; set; }
    }

    public class BookInfo : ItemLookupInfo
    {
        public string SeriesName { get; set; }
    }

    public class SeasonInfo : ItemLookupInfo
    {
        public Dictionary<string, string> SeriesProviderIds { get; set; }
        public int? AnimeSeriesIndex { get; set; }

        public SeasonInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
