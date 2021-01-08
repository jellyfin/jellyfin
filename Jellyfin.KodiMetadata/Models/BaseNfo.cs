#pragma warning disable CA1819

using System;
using System.Collections.Generic;

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
            Genre = Array.Empty<string>();
            Country = Array.Empty<string>();
            Credits = Array.Empty<string>();
            Director = Array.Empty<string>();
            UniqueId = Array.Empty<UniqueIdNfo>();
            Actor = Array.Empty<ActorNfo>();
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the original title.
        /// For example, if the scraper is set to german language and the scraped movie in german is "Der mit dem Wolf tanzt" the original title will be "Dances with Wolves".
        /// </summary>
        public string? OriginalTitle { get; set; }

        /// <summary>
        /// Gets or sets the ratings.
        /// </summary>
        public IEnumerable<RatingNfo>? Ratings { get; set; }

        /// <summary>
        /// Gets or sets the user rating.
        /// </summary>
        public float UserRating { get; set; }

        /// <summary>
        /// Gets or sets the outline. Only used by IMDB.
        /// </summary>
        public string? Outline { get; set; }

        /// <summary>
        /// Gets or sets more informatin about the plot.
        /// </summary>
        public string? Plot { get; set; }

        /// <summary>
        /// Gets or sets the slogan.
        /// </summary>
        public string? Tagline { get; set; }

        /// <summary>
        /// Gets or sets the runtime in minutes.
        /// </summary>
        public int? Runtime { get; set; }

        /// <summary>
        /// Gets or sets the contry specific mpaa rating system.
        /// </summary>
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
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets scraper ids.
        /// </summary>
        public UniqueIdNfo[] UniqueId { get; set; }

        /// <summary>
        /// Gets or sets the genre.
        /// </summary>
        public string[] Genre { get; set; }

        /// <summary>
        /// Gets or sets the country of origin.
        /// </summary>
        public string[] Country { get; set; }

        /// <summary>
        /// Gets or sets item tags.
        /// </summary>
        public string? Tag { get; set; }

        /// <summary>
        /// Gets or sets writers.
        /// </summary>
        public string[] Credits { get; set; }

        /// <summary>
        /// Gets or sets directors.
        /// </summary>
        public string[] Director { get; set; }

        /// <summary>
        /// Gets or sets the release date.
        /// </summary>
        public DateTime? Premiered { get; set; }

        /// <summary>
        /// Gets or sets the release year. Use <see cref="Premiered"/> instead.
        /// </summary>
        [Obsolete("Kodi wiki does encurage to use premiered tag instead")]
        public int? Year { get; set; }

        /// <summary>
        /// Gets or sets the production studio.
        /// </summary>
        public string? Studio { get; set; }

        /// <summary>
        /// Gets or sets the local or online path to the trailer.
        /// </summary>
        public string? Trailer { get; set; }

        /// <summary>
        /// Gets or sets the file info.
        /// </summary>
        public FileInfoNfo? FileInfo { get; set; }

        /// <summary>
        /// Gets or sets the actors.
        /// </summary>
        public ActorNfo[] Actor { get; set; }

        /// <summary>
        /// Gets or sets the resume position.
        /// </summary>
        public ResumePositionNfo? ResumePosition { get; set; }

        /// <summary>
        /// Gets or sets the time the item was added.
        /// </summary>
        public DateTime? DateAdded { get; set; }
    }
}
