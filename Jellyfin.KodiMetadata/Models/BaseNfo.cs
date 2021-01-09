#pragma warning disable CA1819

using System;
using System.Globalization;
using System.Xml.Serialization;

namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The basic nfo tags.
    /// </summary>
    public class BaseNfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseNfo"/> class.
        /// </summary>
        public BaseNfo()
        {
            Genres = Array.Empty<string>();
            Countries = Array.Empty<string>();
            Credits = Array.Empty<string>();
            Directors = Array.Empty<string>();
            UniqueIds = Array.Empty<UniqueIdNfo>();
            Actors = Array.Empty<ActorNfo>();
            Ratings = Array.Empty<RatingNfo>();
            Tags = Array.Empty<string>();
            Writers = Array.Empty<string>();
            Styles = Array.Empty<string>();
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [XmlElement("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the original title.
        /// For example, if the scraper is set to german language and the scraped movie in german is "Der mit dem Wolf tanzt" the original title will be "Dances with Wolves".
        /// </summary>
        [XmlElement("originaltitle")]
        public string? OriginalTitle { get; set; }

        /// <summary>
        /// Gets or sets the ratings.
        /// </summary>
        [XmlArray("ratings")]
        public RatingNfo[] Ratings { get; set; }

        /// <summary>
        /// Gets or sets the user rating.
        /// </summary>
        [XmlElement("userrating")]
        public float UserRating { get; set; }

        /// <summary>
        /// Gets or sets the outline. Only used by IMDB.
        /// </summary>
        [XmlElement("outline")]
        public string? Outline { get; set; }

        /// <summary>
        /// Gets or sets more informatin about the plot.
        /// </summary>
        [XmlElement("plot")]
        public string? Plot { get; set; }

        /// <summary>
        /// Gets or sets the slogan.
        /// </summary>
        [XmlElement("tagline")]
        public string? Tagline { get; set; }

        /// <summary>
        /// Gets or sets the runtime in minutes.
        /// </summary>
        [XmlElement("runtime")]
        public int? Runtime { get; set; }

        /// <summary>
        /// Gets or sets the contry specific mpaa rating system.
        /// </summary>
        [XmlElement("mpaa")]
        public string? Mpaa { get; set; }

        /// <summary>
        /// Gets or sets the number of plays.
        /// </summary>
        [XmlElement("playcount")]
        public int? PlayCount { get; set; }

        /// <summary>
        /// Gets or sets scraper ids.
        /// </summary>
        [XmlElement("uniqueid")]
        public UniqueIdNfo[] UniqueIds { get; set; }

        /// <summary>
        /// Gets or sets the genre.
        /// </summary>
        [XmlElement("genre")]
        public string[] Genres { get; set; }

        /// <summary>
        /// Gets or sets the country of origin.
        /// </summary>
        [XmlElement("country")]
        public string[] Countries { get; set; }

        /// <summary>
        /// Gets or sets item tags.
        /// </summary>
        [XmlElement("tag")]
        public string[] Tags { get; set; }

        /// <summary>
        /// Gets or sets writers.
        /// </summary>
        [XmlElement("credits")]
        public string[] Credits { get; set; }

        /// <summary>
        /// Gets or sets directors.
        /// </summary>
        [XmlElement("director")]
        public string[] Directors { get; set; }

        /// <summary>
        /// Gets or sets the release year. Use <see cref="Premiered"/> instead.
        /// </summary>
        [XmlElement("year")]
        public int? Year { get; set; }

        /// <summary>
        /// Gets or sets the production studio.
        /// </summary>
        [XmlElement("studio")]
        public string? Studio { get; set; }

        /// <summary>
        /// Gets or sets the local or online path to the trailer.
        /// </summary>
        [XmlElement("trailer")]
        public string? Trailer { get; set; }

        /// <summary>
        /// Gets or sets the file info.
        /// </summary>
        [XmlElement("fileinfo")]
        public FileInfoNfo? FileInfo { get; set; }

        /// <summary>
        /// Gets or sets the actors.
        /// </summary>
        [XmlElement("actor")]
        public ActorNfo[] Actors { get; set; }

        /// <summary>
        /// Gets or sets the resume position.
        /// </summary>
        [XmlElement("resumeposition")]
        public ResumePositionNfo? ResumePosition { get; set; }

        [XmlElement("dateadded")]
        public string? DateAddedXml
        {
            get
            {
                return DateAdded?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    DateAdded = date;
                }
            }
        }

        /// <summary>
        /// Gets or sets the time the item was added.
        /// </summary>
        public DateTime? DateAdded { get; set; }

        [XmlElement("lastplayed")]
        public string? LastPlayedXml
        {
            get
            {
                return LastPlayed?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    LastPlayed = date;
                }
            }
        }

        /// <summary>
        /// Gets or sets the last date the file has been played.
        /// </summary>
        public DateTime? LastPlayed { get; set; }

        [XmlElement("premiered")]
        public string? PremieredXml
        {
            get
            {
                return Premiered?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    Premiered = date;
                }
            }
        }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        public DateTime? Released { get; set; }

        [XmlElement("releasedate")]
        public string? ReleasedXml
        {
            get
            {
                return Released?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    Released = date;
                }
            }
        }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        public DateTime? Premiered { get; set; }

        [XmlElement("aired")]
        public string? AiredXml
        {
            get
            {
                return Aired?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    Aired = date;
                }
            }
        }

        public DateTime? Aired { get; set; }

        [XmlElement("formed")]
        public string? FormedXml
        {
            get
            {
                return Formed?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    Formed = date;
                }
            }
        }

        public DateTime? Formed { get; set; }

        [XmlElement("enddate")]
        public string? EndDateXml
        {
            get
            {
                return EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            set
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                {
                    EndDate = date;
                }
            }
        }

        public DateTime? EndDate { get; set; }

        [XmlElement("localtitle")]
        public string? LocalTitle { get; set; }

        /// <summary>
        /// Gets or sets the sort title.
        /// </summary>
        [XmlElement("sorttitle")]
        public string? SortTitle { get; set; }

        [XmlElement("criticrating")]
        public float? CriticRating { get; set; }

        [XmlElement("biography")]
        public string? Biography { get; set; }

        [XmlElement("review")]
        public string? Review { get; set; }

        [XmlElement("language")]
        public string? Language { get; set; }

        [XmlElement("countrycode")]
        public string? CountryCode { get; set; }

        [XmlElement("lockedfields")]
        public string? LockedFields { get; set; }

        [XmlElement("customrating")]
        public string? CustomRating { get; set; }

        [XmlElement("aspectratio")]
        public string? AspectRatio { get; set; }

        [XmlElement("lockdata")]
        public bool LockData { get; set; }

        [XmlElement("writer")]
        public string[] Writers { get; set; }

        [XmlElement("displayorder")]
        public string? DisplayOrder { get; set; }

        [XmlElement("rating")]
        public float? Rating { get; set; }

        [XmlElement("style")]
        public string[] Styles { get; set; }

        [XmlElement("art")]
        public ArtNfo? Art { get; set; }

        // Provider Ids

        [XmlElement("collectionnumber")]
        [XmlElement("tmdbcolid")]
        public string? CollectionId { get; set; }

        [XmlElement("imdbid")]
        public string? ImdbId { get; set; }

        [XmlElement("tmdbid")]
        public string? TmdbId { get; set; }

        [XmlElement("tvdbid")]
        public string? TvdbId { get; set; }

        [XmlElement("tvcomid")]
        public string? TvcomId { get; set; }

        [XmlElement("musicbrainzalbumid")]
        public string? MusicBrainzAlbumId { get; set; }

        [XmlElement("musicbrainzalbumartistid")]
        public string? MusicBrainzAlbumArtistId { get; set; }

        [XmlElement("musicbrainzartistid")]
        public string? MusicBrainzArtistId { get; set; }

        [XmlElement("musicbrainzreleasegroupid")]
        public string? MusicBrainzReleaseGroupId { get; set; }

        [XmlElement("zap2itid")]
        public string? Zap2ItId { get; set; }

        [XmlElement("tvrageid")]
        public string? TvRageId { get; set; }

        [XmlElement("audiodbartistid")]
        public string? AudioDbArtistId { get; set; }

        [XmlElement("audiodbalbumid")]
        public string? AudioDbAlbumId { get; set; }

        [XmlElement("musicbrainztrackid")]
        public string? MusicBrainzTrackId { get; set; }

        [XmlElement("tvmazeid")]
        public string? TvMazeId { get; set; }
    }
}
