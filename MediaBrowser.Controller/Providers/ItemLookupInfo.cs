using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public List<string> AlbumArtists { get; set; }

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
            AlbumArtists = new List<string>();
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

    public class EpisodeInfo : ItemLookupInfo, IHasIdentities<EpisodeIdentity>
    {
        private List<EpisodeIdentity> _identities = new List<EpisodeIdentity>();

        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public int? IndexNumberEnd { get; set; }
        public int? AnimeSeriesIndex { get; set; }

        public EpisodeInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<EpisodeIdentity> Identities
        {
            get { return _identities; }
        }

        public async Task FindIdentities(IProviderManager providerManager, CancellationToken cancellationToken)
        {
            var identifier = new ItemIdentifier<EpisodeInfo, EpisodeIdentity>();
            _identities = (await identifier.FindIdentities(this, providerManager, cancellationToken)).ToList();
        }
    }

    public class EpisodeIdentity : IItemIdentity
    {
        public string Type { get; set; }

        public string SeriesId { get; set; }
        public int? SeasonIndex { get; set; }
        public int IndexNumber { get; set; }
        public int? IndexNumberEnd { get; set; }
    }

    public class SongInfo : ItemLookupInfo
    {
        public List<string> AlbumArtists { get; set; }
        public string Album { get; set; }
        public List<string> Artists { get; set; }

        public SongInfo()
        {
            Artists = new List<string>();
            AlbumArtists = new List<string>();
        }
    }

    public class SeriesInfo : ItemLookupInfo, IHasIdentities<SeriesIdentity>
    {
        private List<SeriesIdentity> _identities = new List<SeriesIdentity>();

        public int? AnimeSeriesIndex { get; set; }

        public IEnumerable<SeriesIdentity> Identities
        {
            get { return _identities; }
        }

        public async Task FindIdentities(IProviderManager providerManager, CancellationToken cancellationToken)
        {
            var identifier = new ItemIdentifier<SeriesInfo, SeriesIdentity>();
            _identities = (await identifier.FindIdentities(this, providerManager, cancellationToken)).ToList();
        }
    }

    public class SeriesIdentity : IItemIdentity
    {
        public string Type { get; set; }

        public string Id { get; set; }
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

    public class SeasonInfo : ItemLookupInfo, IHasIdentities<SeasonIdentity>
    {
        private List<SeasonIdentity> _identities = new List<SeasonIdentity>();

        public Dictionary<string, string> SeriesProviderIds { get; set; }
        public int? AnimeSeriesIndex { get; set; }

        public SeasonInfo()
        {
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<SeasonIdentity> Identities
        {
            get { return _identities; }
        }

        public async Task FindIdentities(IProviderManager providerManager, CancellationToken cancellationToken)
        {
            var identifier = new ItemIdentifier<SeasonInfo, SeasonIdentity>();
            _identities = (await identifier.FindIdentities(this, providerManager, cancellationToken)).ToList();
        }
    }

    public class SeasonIdentity : IItemIdentity
    {
        public string Type { get; set; }

        public string SeriesId { get; set; }

        public int SeasonIndex { get; set; }
    }
}
