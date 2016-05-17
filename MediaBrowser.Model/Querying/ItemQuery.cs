using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Contains all the possible parameters that can be used to query for items
    /// </summary>
    public class ItemQuery
    {
        /// <summary>
        /// The user to localize search results for
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        public string ParentId { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// What to sort the results by
        /// </summary>
        /// <value>The sort by.</value>
        public string[] SortBy { get; set; }

        /// <summary>
        /// Gets or sets the artist ids.
        /// </summary>
        /// <value>The artist ids.</value>
        public string[] ArtistIds { get; set; }
        
        /// <summary>
        /// The sort order to return results with
        /// </summary>
        /// <value>The sort order.</value>
        public SortOrder? SortOrder { get; set; }

        /// <summary>
        /// Filters to apply to the results
        /// </summary>
        /// <value>The filters.</value>
        public ItemFilter[] Filters { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }

        /// <summary>
        /// Gets or sets the media types.
        /// </summary>
        /// <value>The media types.</value>
        public string[] MediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the video formats.
        /// </summary>
        /// <value>The video formats.</value>
        public bool? Is3D { get; set; }

        /// <summary>
        /// Gets or sets the video types.
        /// </summary>
        /// <value>The video types.</value>
        public VideoType[] VideoTypes { get; set; }

        /// <summary>
        /// Whether or not to perform the query recursively
        /// </summary>
        /// <value><c>true</c> if recursive; otherwise, <c>false</c>.</value>
        public bool Recursive { get; set; }

        /// <summary>
        /// Limit results to items containing specific genres
        /// </summary>
        /// <value>The genres.</value>
        public string[] Genres { get; set; }

        /// <summary>
        /// Gets or sets the studio ids.
        /// </summary>
        /// <value>The studio ids.</value>
        public string[] StudioIds { get; set; }

        /// <summary>
        /// Gets or sets the exclude item types.
        /// </summary>
        /// <value>The exclude item types.</value>
        public string[] ExcludeItemTypes { get; set; }

        /// <summary>
        /// Gets or sets the include item types.
        /// </summary>
        /// <value>The include item types.</value>
        public string[] IncludeItemTypes { get; set; }

        /// <summary>
        /// Limit results to items containing specific years
        /// </summary>
        /// <value>The years.</value>
        public int[] Years { get; set; }

        /// <summary>
        /// Limit results to items containing a specific person
        /// </summary>
        /// <value>The person.</value>
        public string[] PersonIds { get; set; }

        /// <summary>
        /// If the Person filter is used, this can also be used to restrict to a specific person type
        /// </summary>
        /// <value>The type of the person.</value>
        public string[] PersonTypes { get; set; }

        /// <summary>
        /// Search characters used to find items
        /// </summary>
        /// <value>The index by.</value>
        public string SearchTerm { get; set; }

        /// <summary>
        /// Gets or sets the image types.
        /// </summary>
        /// <value>The image types.</value>
        public ImageType[] ImageTypes { get; set; }

        /// <summary>
        /// Gets or sets the air days.
        /// </summary>
        /// <value>The air days.</value>
        public DayOfWeek[] AirDays { get; set; }

        /// <summary>
        /// Gets or sets the series status.
        /// </summary>
        /// <value>The series status.</value>
        public SeriesStatus[] SeriesStatuses { get; set; }

        /// <summary>
        /// Gets or sets the ids, which are specific items to retrieve
        /// </summary>
        /// <value>The ids.</value>
        public string[] Ids { get; set; }

        /// <summary>
        /// Gets or sets the min official rating.
        /// </summary>
        /// <value>The min official rating.</value>
        public string MinOfficialRating { get; set; }

        /// <summary>
        /// Gets or sets the max official rating.
        /// </summary>
        /// <value>The max official rating.</value>
        public string MaxOfficialRating { get; set; }

        /// <summary>
        /// Gets or sets the min index number.
        /// </summary>
        /// <value>The min index number.</value>
        public int? MinIndexNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has parental rating.
        /// </summary>
        /// <value><c>null</c> if [has parental rating] contains no value, <c>true</c> if [has parental rating]; otherwise, <c>false</c>.</value>
        public bool? HasParentalRating { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is HD.
        /// </summary>
        /// <value><c>null</c> if [is HD] contains no value, <c>true</c> if [is HD]; otherwise, <c>false</c>.</value>
        public bool? IsHD { get; set; }

        /// <summary>
        /// Gets or sets the parent index number.
        /// </summary>
        /// <value>The parent index number.</value>
        public int? ParentIndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the min players.
        /// </summary>
        /// <value>The min players.</value>
        public int? MinPlayers { get; set; }

        /// <summary>
        /// Gets or sets the max players.
        /// </summary>
        /// <value>The max players.</value>
        public int? MaxPlayers { get; set; }

        /// <summary>
        /// Gets or sets the name starts with or greater.
        /// </summary>
        /// <value>The name starts with or greater.</value>
        public string NameStartsWithOrGreater { get; set; }

        /// <summary>
        /// Gets or sets the name starts with.
        /// </summary>
        /// <value>The name starts with or greater.</value>
        public string NameStartsWith { get; set; }

        /// <summary>
        /// Gets or sets the name starts with.
        /// </summary>
        /// <value>The name lessthan.</value>
        public string NameLessThan { get; set; }

        /// <summary>
        /// Gets or sets the album artist starts with or greater.
        /// </summary>
        /// <value>The album artist starts with or greater.</value>
        public string AlbumArtistStartsWithOrGreater { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include index containers].
        /// </summary>
        /// <value><c>true</c> if [include index containers]; otherwise, <c>false</c>.</value>
        public bool IncludeIndexContainers { get; set; }

        /// <summary>
        /// Gets or sets the location types.
        /// </summary>
        /// <value>The location types.</value>
        public LocationType[] LocationTypes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is missing episode.
        /// </summary>
        /// <value><c>null</c> if [is missing episode] contains no value, <c>true</c> if [is missing episode]; otherwise, <c>false</c>.</value>
        public bool? IsMissing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is unaired episode.
        /// </summary>
        /// <value><c>null</c> if [is unaired episode] contains no value, <c>true</c> if [is unaired episode]; otherwise, <c>false</c>.</value>
        public bool? IsUnaired { get; set; }

        public bool? IsVirtualUnaired { get; set; }

        public bool? IsInBoxSet { get; set; }

        public bool? CollapseBoxSetItems { get; set; }

        public bool? IsPlayed { get; set; }

        /// <summary>
        /// Gets or sets the exclude location types.
        /// </summary>
        /// <value>The exclude location types.</value>
        public LocationType[] ExcludeLocationTypes { get; set; }

        public double? MinCommunityRating { get; set; }
        public double? MinCriticRating { get; set; }

        public int? AiredDuringSeason { get; set; }

        public DateTime? MinPremiereDate { get; set; }

        public DateTime? MaxPremiereDate { get; set; }

        public bool? EnableImages { get; set; }
        public int? ImageTypeLimit { get; set; }
        public ImageType[] EnableImageTypes { get; set; }

        [Obsolete]
        public string[] Artists { get; set; }
        [Obsolete]
        public string[] Studios { get; set; }
        [Obsolete]
        public string Person { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemQuery" /> class.
        /// </summary>
        public ItemQuery()
        {
            LocationTypes = new LocationType[] { };
            ExcludeLocationTypes = new LocationType[] { };

            SortBy = new string[] { };

            Filters = new ItemFilter[] { };

            Fields = new ItemFields[] { };

            MediaTypes = new string[] { };

            VideoTypes = new VideoType[] { };

            EnableTotalRecordCount = true;

            Artists = new string[] { };
            Studios = new string[] { };
            
            Genres = new string[] { };
            StudioIds = new string[] { };
            IncludeItemTypes = new string[] { };
            ExcludeItemTypes = new string[] { };
            Years = new int[] { };
            PersonTypes = new string[] { };
            Ids = new string[] { };
            ArtistIds = new string[] { };
            PersonIds = new string[] { };

            ImageTypes = new ImageType[] { };
            AirDays = new DayOfWeek[] { };
            SeriesStatuses = new SeriesStatus[] { };
            EnableImageTypes = new ImageType[] { };
        }
    }
}
