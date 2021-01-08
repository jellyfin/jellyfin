#pragma warning disable CA1819

using System;
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
        protected BaseNfo()
        {
            Genres = Array.Empty<string>();
            Countries = Array.Empty<string>();
            Credits = Array.Empty<string>();
            Directors = Array.Empty<string>();
            UniqueIds = Array.Empty<UniqueIdNfo>();
            Actors = Array.Empty<ActorNfo>();
            Ratings = Array.Empty<RatingNfo>();
            Tags = Array.Empty<string>();
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
        /// Gets the ratings.
        /// </summary>
        public RatingNfo[] Ratings { get; }

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
        public int? PlayCount { get; set; }

        /// <summary>
        /// Gets or sets the last <see cref="DateTime"/> the file has been played.
        /// </summary>
        public DateTime? LastPlayed { get; set; }

        /// <summary>
        /// Gets or sets the imdb id.
        /// </summary>
        [XmlElement("id")]
        public string? Id { get; set; }

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
        /// Gets or sets the release date.
        /// </summary>
        [XmlElement("premiered")]
        public DateTime? Premiered { get; set; }

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

        /// <summary>
        /// Gets or sets the time the item was added.
        /// </summary>
        public DateTime? DateAdded { get; set; }
    }
}
