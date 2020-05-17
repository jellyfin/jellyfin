using System;
using System.Linq;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api.UserLibrary
{
    public abstract class BaseItemsRequest : IHasDtoOptions
    {
        protected BaseItemsRequest()
        {
            EnableImages = true;
            EnableTotalRecordCount = true;
        }

        /// <summary>
        /// Gets or sets the max offical rating.
        /// </summary>
        /// <value>The max offical rating.</value>
        [ApiMember(Name = "MaxOfficialRating", Description = "Optional filter by maximum official rating (PG, PG-13, TV-MA, etc).", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MaxOfficialRating { get; set; }

        [ApiMember(Name = "HasThemeSong", Description = "Optional filter by items with theme songs.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasThemeSong { get; set; }

        [ApiMember(Name = "HasThemeVideo", Description = "Optional filter by items with theme videos.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasThemeVideo { get; set; }

        [ApiMember(Name = "HasSubtitles", Description = "Optional filter by items with subtitles.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasSubtitles { get; set; }

        [ApiMember(Name = "HasSpecialFeature", Description = "Optional filter by items with special features.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasSpecialFeature { get; set; }

        [ApiMember(Name = "HasTrailer", Description = "Optional filter by items with trailers.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasTrailer { get; set; }

        [ApiMember(Name = "AdjacentTo", Description = "Optional. Return items that are siblings of a supplied item.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string AdjacentTo { get; set; }

        [ApiMember(Name = "MinIndexNumber", Description = "Optional filter by minimum index number.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MinIndexNumber { get; set; }

        [ApiMember(Name = "ParentIndexNumber", Description = "Optional filter by parent index number.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ParentIndexNumber { get; set; }

        [ApiMember(Name = "HasParentalRating", Description = "Optional filter by items that have or do not have a parental rating", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasParentalRating { get; set; }

        [ApiMember(Name = "IsHD", Description = "Optional filter by items that are HD or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsHD { get; set; }

        public bool? Is4K { get; set; }

        [ApiMember(Name = "LocationTypes", Description = "Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string LocationTypes { get; set; }

        [ApiMember(Name = "ExcludeLocationTypes", Description = "Optional. If specified, results will be filtered based on LocationType. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ExcludeLocationTypes { get; set; }

        [ApiMember(Name = "IsMissing", Description = "Optional filter by items that are missing episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsMissing { get; set; }

        [ApiMember(Name = "IsUnaired", Description = "Optional filter by items that are unaired episodes or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsUnaired { get; set; }

        [ApiMember(Name = "MinCommunityRating", Description = "Optional filter by minimum community rating.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public double? MinCommunityRating { get; set; }

        [ApiMember(Name = "MinCriticRating", Description = "Optional filter by minimum critic rating.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public double? MinCriticRating { get; set; }

        [ApiMember(Name = "AiredDuringSeason", Description = "Gets all episodes that aired during a season, including specials.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? AiredDuringSeason { get; set; }

        [ApiMember(Name = "MinPremiereDate", Description = "Optional. The minimum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MinPremiereDate { get; set; }

        [ApiMember(Name = "MinDateLastSaved", Description = "Optional. The minimum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MinDateLastSaved { get; set; }

        [ApiMember(Name = "MinDateLastSavedForUser", Description = "Optional. The minimum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MinDateLastSavedForUser { get; set; }

        [ApiMember(Name = "MaxPremiereDate", Description = "Optional. The maximum premiere date. Format = ISO", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MaxPremiereDate { get; set; }

        [ApiMember(Name = "HasOverview", Description = "Optional filter by items that have an overview or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasOverview { get; set; }

        [ApiMember(Name = "HasImdbId", Description = "Optional filter by items that have an imdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasImdbId { get; set; }

        [ApiMember(Name = "HasTmdbId", Description = "Optional filter by items that have a tmdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasTmdbId { get; set; }

        [ApiMember(Name = "HasTvdbId", Description = "Optional filter by items that have a tvdb id or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? HasTvdbId { get; set; }

        [ApiMember(Name = "ExcludeItemIds", Description = "Optional. If specified, results will be filtered by exxcluding item ids. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ExcludeItemIds { get; set; }

        public bool EnableTotalRecordCount { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        /// <summary>
        /// Whether or not to perform the query recursively
        /// </summary>
        /// <value><c>true</c> if recursive; otherwise, <c>false</c>.</value>
        [ApiMember(Name = "Recursive", Description = "When searching within folders, this determines whether or not the search will be recursive. true/false", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool Recursive { get; set; }

        public string SearchTerm { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>The sort order.</value>
        [ApiMember(Name = "SortOrder", Description = "Sort Order - Ascending,Descending", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string SortOrder { get; set; }

        /// <summary>
        /// Specify this to localize the search to a specific item or folder. Omit to use the root.
        /// </summary>
        /// <value>The parent id.</value>
        [ApiMember(Name = "ParentId", Description = "Specify this to localize the search to a specific item or folder. Omit to use the root", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        [ApiMember(Name = "Fields", Description = "Optional. Specify additional fields of information to return in the output. This allows multiple, comma delimeted. Options: Budget, Chapters, DateCreated, Genres, HomePageUrl, IndexOptions, MediaStreams, Overview, ParentId, Path, People, ProviderIds, PrimaryImageAspectRatio, Revenue, SortName, Studios, Taglines", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Fields { get; set; }

        /// <summary>
        /// Gets or sets the exclude item types.
        /// </summary>
        /// <value>The exclude item types.</value>
        [ApiMember(Name = "ExcludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ExcludeItemTypes { get; set; }

        /// <summary>
        /// Gets or sets the include item types.
        /// </summary>
        /// <value>The include item types.</value>
        [ApiMember(Name = "IncludeItemTypes", Description = "Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string IncludeItemTypes { get; set; }

        /// <summary>
        /// Filters to apply to the results
        /// </summary>
        /// <value>The filters.</value>
        [ApiMember(Name = "Filters", Description = "Optional. Specify additional filters to apply. This allows multiple, comma delimeted. Options: IsFolder, IsNotFolder, IsUnplayed, IsPlayed, IsFavorite, IsResumable, Likes, Dislikes", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Filters { get; set; }

        /// <summary>
        /// Gets or sets the Isfavorite option
        /// </summary>
        /// <value>IsFavorite</value>
        [ApiMember(Name = "IsFavorite", Description = "Optional filter by items that are marked as favorite, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets the media types.
        /// </summary>
        /// <value>The media types.</value>
        [ApiMember(Name = "MediaTypes", Description = "Optional filter by MediaType. Allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string MediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the image types.
        /// </summary>
        /// <value>The image types.</value>
        [ApiMember(Name = "ImageTypes", Description = "Optional. If specified, results will be filtered based on those containing image types. This allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ImageTypes { get; set; }

        /// <summary>
        /// What to sort the results by
        /// </summary>
        /// <value>The sort by.</value>
        [ApiMember(Name = "SortBy", Description = "Optional. Specify one or more sort orders, comma delimeted. Options: Album, AlbumArtist, Artist, Budget, CommunityRating, CriticRating, DateCreated, DatePlayed, PlayCount, PremiereDate, ProductionYear, SortName, Random, Revenue, Runtime", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SortBy { get; set; }

        [ApiMember(Name = "IsPlayed", Description = "Optional filter by items that are played, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsPlayed { get; set; }

        /// <summary>
        /// Limit results to items containing specific genres
        /// </summary>
        /// <value>The genres.</value>
        [ApiMember(Name = "Genres", Description = "Optional. If specified, results will be filtered based on genre. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Genres { get; set; }

        public string GenreIds { get; set; }

        [ApiMember(Name = "OfficialRatings", Description = "Optional. If specified, results will be filtered based on OfficialRating. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string OfficialRatings { get; set; }

        [ApiMember(Name = "Tags", Description = "Optional. If specified, results will be filtered based on tag. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Tags { get; set; }

        /// <summary>
        /// Limit results to items containing specific years
        /// </summary>
        /// <value>The years.</value>
        [ApiMember(Name = "Years", Description = "Optional. If specified, results will be filtered based on production year. This allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Years { get; set; }

        [ApiMember(Name = "EnableImages", Description = "Optional, include image information in output", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableImages { get; set; }

        [ApiMember(Name = "EnableUserData", Description = "Optional, include user data", IsRequired = false, DataType = "boolean", ParameterType = "query", Verb = "GET")]
        public bool? EnableUserData { get; set; }

        [ApiMember(Name = "ImageTypeLimit", Description = "Optional, the max number of images to return, per image type", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? ImageTypeLimit { get; set; }

        [ApiMember(Name = "EnableImageTypes", Description = "Optional. The image types to include in the output.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string EnableImageTypes { get; set; }

        /// <summary>
        /// Limit results to items containing a specific person
        /// </summary>
        /// <value>The person.</value>
        [ApiMember(Name = "Person", Description = "Optional. If specified, results will be filtered to include only those containing the specified person.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Person { get; set; }

        [ApiMember(Name = "PersonIds", Description = "Optional. If specified, results will be filtered to include only those containing the specified person.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string PersonIds { get; set; }

        /// <summary>
        /// If the Person filter is used, this can also be used to restrict to a specific person type
        /// </summary>
        /// <value>The type of the person.</value>
        [ApiMember(Name = "PersonTypes", Description = "Optional. If specified, along with Person, results will be filtered to include only those containing the specified person and PersonType. Allows multiple, comma-delimited", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string PersonTypes { get; set; }

        /// <summary>
        /// Limit results to items containing specific studios
        /// </summary>
        /// <value>The studios.</value>
        [ApiMember(Name = "Studios", Description = "Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Studios { get; set; }

        [ApiMember(Name = "StudioIds", Description = "Optional. If specified, results will be filtered based on studio. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string StudioIds { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        [ApiMember(Name = "Artists", Description = "Optional. If specified, results will be filtered based on artist. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Artists { get; set; }

        public string ExcludeArtistIds { get; set; }

        [ApiMember(Name = "ArtistIds", Description = "Optional. If specified, results will be filtered based on artist. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string ArtistIds { get; set; }

        public string AlbumArtistIds { get; set; }

        public string ContributingArtistIds { get; set; }

        [ApiMember(Name = "Albums", Description = "Optional. If specified, results will be filtered based on album. This allows multiple, pipe delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Albums { get; set; }

        public string AlbumIds { get; set; }

        /// <summary>
        /// Gets or sets the item ids.
        /// </summary>
        /// <value>The item ids.</value>
        [ApiMember(Name = "Ids", Description = "Optional. If specific items are needed, specify a list of item id's to retrieve. This allows multiple, comma delimited.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string Ids { get; set; }

        /// <summary>
        /// Gets or sets the video types.
        /// </summary>
        /// <value>The video types.</value>
        [ApiMember(Name = "VideoTypes", Description = "Optional filter by VideoType (videofile, dvd, bluray, iso). Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string VideoTypes { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the min offical rating.
        /// </summary>
        /// <value>The min offical rating.</value>
        [ApiMember(Name = "MinOfficialRating", Description = "Optional filter by minimum official rating (PG, PG-13, TV-MA, etc).", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string MinOfficialRating { get; set; }

        [ApiMember(Name = "IsLocked", Description = "Optional filter by items that are locked.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? IsLocked { get; set; }

        [ApiMember(Name = "IsPlaceHolder", Description = "Optional filter by items that are placeholders", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? IsPlaceHolder { get; set; }

        [ApiMember(Name = "HasOfficialRating", Description = "Optional filter by items that have official ratings", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public bool? HasOfficialRating { get; set; }

        [ApiMember(Name = "CollapseBoxSetItems", Description = "Whether or not to hide items behind their boxsets.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? CollapseBoxSetItems { get; set; }

        public int? MinWidth { get; set; }
        public int? MinHeight { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the video formats.
        /// </summary>
        /// <value>The video formats.</value>
        [ApiMember(Name = "Is3D", Description = "Optional filter by items that are 3D, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? Is3D { get; set; }

        /// <summary>
        /// Gets or sets the series status.
        /// </summary>
        /// <value>The series status.</value>
        [ApiMember(Name = "SeriesStatus", Description = "Optional filter by Series Status. Allows multiple, comma delimeted.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET", AllowMultiple = true)]
        public string SeriesStatus { get; set; }

        [ApiMember(Name = "NameStartsWithOrGreater", Description = "Optional filter by items whose name is sorted equally or greater than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameStartsWithOrGreater { get; set; }

        [ApiMember(Name = "NameStartsWith", Description = "Optional filter by items whose name is sorted equally than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameStartsWith { get; set; }

        [ApiMember(Name = "NameLessThan", Description = "Optional filter by items whose name is equally or lesser than a given input string.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string NameLessThan { get; set; }

        public string[] GetGenres()
        {
            return (Genres ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetTags()
        {
            return (Tags ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetOfficialRatings()
        {
            return (OfficialRatings ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetMediaTypes()
        {
            return (MediaTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetIncludeItemTypes()
        {
            return (IncludeItemTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetExcludeItemTypes()
        {
            return (ExcludeItemTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public int[] GetYears()
        {
            return (Years ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
        }

        public string[] GetStudios()
        {
            return (Studios ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetPersonTypes()
        {
            return (PersonTypes ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public VideoType[] GetVideoTypes()
        {
            return string.IsNullOrEmpty(VideoTypes)
                ? Array.Empty<VideoType>()
                : VideoTypes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => Enum.Parse<VideoType>(v, true)).ToArray();
        }

        /// <summary>
        /// Gets the filters.
        /// </summary>
        /// <returns>IEnumerable{ItemFilter}.</returns>
        public ItemFilter[] GetFilters()
        {
            var val = Filters;

            return string.IsNullOrEmpty(val)
                ? Array.Empty<ItemFilter>()
                : val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => Enum.Parse<ItemFilter>(v, true)).ToArray();
        }

        /// <summary>
        /// Gets the image types.
        /// </summary>
        /// <returns>IEnumerable{ImageType}.</returns>
        public ImageType[] GetImageTypes()
        {
            var val = ImageTypes;

            return string.IsNullOrEmpty(val)
                ? Array.Empty<ImageType>()
                : val.Split(',').Select(v => Enum.Parse<ImageType>(v, true)).ToArray();
        }

        /// <summary>
        /// Gets the order by.
        /// </summary>
        /// <returns>IEnumerable{ItemSortBy}.</returns>
        public ValueTuple<string, SortOrder>[] GetOrderBy()
        {
            return GetOrderBy(SortBy, SortOrder);
        }

        public static ValueTuple<string, SortOrder>[] GetOrderBy(string sortBy, string requestedSortOrder)
        {
            var val = sortBy;

            if (string.IsNullOrEmpty(val))
            {
                return Array.Empty<ValueTuple<string, SortOrder>>();
            }

            var vals = val.Split(',');
            if (string.IsNullOrWhiteSpace(requestedSortOrder))
            {
                requestedSortOrder = "Ascending";
            }

            var sortOrders = requestedSortOrder.Split(',');

            var result = new ValueTuple<string, SortOrder>[vals.Length];

            for (var i = 0; i < vals.Length; i++)
            {
                var sortOrderIndex = sortOrders.Length > i ? i : 0;

                var sortOrderValue = sortOrders.Length > sortOrderIndex ? sortOrders[sortOrderIndex] : null;
                var sortOrder = string.Equals(sortOrderValue, "Descending", StringComparison.OrdinalIgnoreCase)
                    ? MediaBrowser.Model.Entities.SortOrder.Descending
                    : MediaBrowser.Model.Entities.SortOrder.Ascending;

                result[i] = new ValueTuple<string, SortOrder>(vals[i], sortOrder);
            }

            return result;
        }
    }
}
