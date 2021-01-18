#pragma warning disable CA1819

using System;
using System.Globalization;
using System.Xml.Serialization;
using MediaBrowser.Controller.Serialization;

namespace Jellyfin.NfoMetadata.Models
{
    /// <summary>
    /// The basic nfo tags.
    /// </summary>
    public class BaseNfo
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [XmlElement("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [XmlElement("name")]
        public string? Name { get; set; }

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
        public RatingNfo[]? Ratings { get; set; }

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
        public long? Runtime { get; set; }

        /// <summary>
        /// Gets or sets the contry specific mpaa rating system.
        /// </summary>
        [XmlElement("mpaa")]
        public string? Mpaa { get; set; }

        /// <summary>
        /// Gets or sets the number of plays.
        /// has to be a string because tinyMediaManager saves it as an empty tag and XmlSerializer can't deserialize <playcount /> into an int?.
        /// </summary>
        [XmlElement("playcount")]
        public string? PlayCount { get; set; }

        /// <summary>
        /// Gets or sets scraper ids.
        /// </summary>
        [XmlElement("uniqueid")]
        public UniqueIdNfo[]? UniqueIds { get; set; }

        /// <summary>
        /// Gets or sets the genre.
        /// </summary>
        [XmlElement("genre")]
        public string[]? Genres { get; set; }

        /// <summary>
        /// Gets or sets the country of origin.
        /// </summary>
        [XmlElement("country")]
        public string[]? Countries { get; set; }

        /// <summary>
        /// Gets or sets item tags.
        /// </summary>
        [XmlElement("tag")]
        public string[]? Tags { get; set; }

        /// <summary>
        /// Gets or sets writers.
        /// </summary>
        [XmlElement("credits")]
        public string[]? Credits { get; set; }

        /// <summary>
        /// Gets or sets directors.
        /// </summary>
        [XmlElement("director")]
        public string[]? Directors { get; set; }

        /// <summary>
        /// Gets or sets the release year. Use <see cref="Premiered"/> instead.
        /// </summary>
        [XmlElement("year")]
        public int? Year { get; set; }

        /// <summary>
        /// Gets or sets the production studios.
        /// </summary>
        [XmlElement("studio")]
        public string[]? Studios { get; set; }

        /// <summary>
        /// Gets or sets the local or online path to the trailer.
        /// </summary>
        [XmlElement("trailer")]
        public string[]? Trailers { get; set; }

        /// <summary>
        /// Gets or sets the file info.
        /// </summary>
        [XmlElement("fileinfo")]
        public FileInfoNfo? FileInfo { get; set; }

        /// <summary>
        /// Gets or sets the actors.
        /// </summary>
        [XmlElement("actor")]
        public ActorNfo[]? Actors { get; set; }

        /// <summary>
        /// Gets or sets the resume position.
        /// </summary>
        [XmlElement("resume")]
        [XmlSynonyms("resumeposition")]
        public ResumePositionNfo? ResumePosition { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DateAdded"/> property. THIS IS ONLY USED FOR THE XML SERIALIZER.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the <see cref="LastPlayed"/> property. THIS IS ONLY USED FOR THE XML SERIALIZER.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the <see cref="Premiered"/> property. THIS IS ONLY USED FOR THE XML SERIALIZER.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the <see cref="Released"/> property. THIS IS ONLY USED FOR THE XML SERIALIZER.THIS IS ONLY USED FOR THE XML SERIALIZER.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the <see cref="Aired"/> property. THIS IS ONLY USED FOR THE XML SERIALIZER.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the aired date.
        /// </summary>
        public DateTime? Aired { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Formed"/> property. THIS IS ONLY USED FOR THE XML SERIALIZER.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the formed date.
        /// </summary>
        public DateTime? Formed { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EndDate"/> property. THIS IS ONLY USED FOR THE XML SERIALIZER.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the local title of the item.
        /// </summary>
        [XmlElement("localtitle")]
        public string? LocalTitle { get; set; }

        /// <summary>
        /// Gets or sets the sort title.
        /// </summary>
        [XmlElement("sorttitle")]
        public string? SortTitle { get; set; }

        /// <summary>
        /// Gets or sets the sort name.
        /// </summary>
        [XmlElement("sortname")]
        public string? SortName { get; set; }

        /// <summary>
        /// Gets or sets the critic rating.
        /// </summary>
        [XmlElement("criticrating")]
        public float? CriticRating { get; set; }

        /// <summary>
        /// Gets or sets the biography.
        /// </summary>
        [XmlElement("biography")]
        public string? Biography { get; set; }

        /// <summary>
        /// Gets or sets the item review.
        /// </summary>
        [XmlElement("review")]
        public string? Review { get; set; }

        /// <summary>
        /// Gets or sets the item language.
        /// </summary>
        [XmlElement("language")]
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the item language item code.
        /// </summary>
        [XmlElement("countrycode")]
        public string? CountryCode { get; set; }

        /// <summary>
        /// Gets or sets the locked item fields. Pipe seperated.
        /// </summary>
        [XmlElement("lockedfields")]
        public string? LockedFields { get; set; }

        /// <summary>
        /// Gets or sets the custom user rating.
        /// </summary>
        [XmlElement("customrating")]
        public string? CustomRating { get; set; }

        /// <summary>
        /// Gets or sets the item aspect ratio.
        /// </summary>
        [XmlElement("aspectratio")]
        public string? AspectRatio { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to lock all the item data.
        /// </summary>
        [XmlElement("lockdata")]
        public bool LockData { get; set; }

        /// <summary>
        /// Gets or sets the writers.
        /// </summary>
        [XmlElement("writer")]
        public string[]? Writers { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        [XmlElement("displayorder")]
        public string? DisplayOrder { get; set; }

        /// <summary>
        /// Gets or sets the item rating.
        /// </summary>
        [XmlElement("rating")]
        public float? Rating { get; set; }

        /// <summary>
        /// Gets or sets the item styles.
        /// </summary>
        [XmlElement("style")]
        public string[]? Styles { get; set; }

        /// <summary>
        /// Gets or sets the item art.
        /// </summary>
        [XmlElement("art")]
        public ArtNfo? Art { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item is a user favorite.
        /// </summary>
        [XmlElement("isuserfavorite")]
        public bool IsUserFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user has watched the item.
        /// </summary>
        [XmlElement("watched")]
        public bool Watched { get; set; }

        /// <summary>
        /// Gets or sets the collection items.
        /// </summary>
        [XmlArray("collectionitem")]
        public CollectionItemNfo[]? CollectionItems { get; set; }

        // Provider Ids

        [XmlElement("collectionnumber")]
        [XmlSynonyms("tmdbcolid")]
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
