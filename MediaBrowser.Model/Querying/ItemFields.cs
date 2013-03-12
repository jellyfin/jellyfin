
namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Used to control the data that gets attached to DtoBaseItems
    /// </summary>
    public enum ItemFields
    {
        /// <summary>
        /// Audio properties
        /// </summary>
        AudioInfo,

        /// <summary>
        /// The chapters
        /// </summary>
        Chapters,

        /// <summary>
        /// The date created of the item
        /// </summary>
        DateCreated,

        /// <summary>
        /// The display media type
        /// </summary>
        DisplayMediaType,

        /// <summary>
        /// Item display preferences
        /// </summary>
        DisplayPreferences,

        /// <summary>
        /// Genres
        /// </summary>
        Genres,

        /// <summary>
        /// Child count, recursive child count, etc
        /// </summary>
        ItemCounts,

        /// <summary>
        /// The fields that the server supports indexing on
        /// </summary>
        IndexOptions,

        /// <summary>
        /// The item overview
        /// </summary>
        Overview,

        /// <summary>
        /// The overview HTML
        /// </summary>
        OverviewHtml,
        
        /// <summary>
        /// The id of the item's parent
        /// </summary>
        ParentId,

        /// <summary>
        /// The physical path of the item
        /// </summary>
        Path,

        /// <summary>
        /// The list of people for the item
        /// </summary>
        People,

        /// <summary>
        /// Imdb, tmdb, etc
        /// </summary>
        ProviderIds,

        /// <summary>
        /// The aspect ratio of the primary image
        /// </summary>
        PrimaryImageAspectRatio,

        /// <summary>
        /// AirDays, status, SeriesName, etc
        /// </summary>
        SeriesInfo,

        /// <summary>
        /// The sort name of the item
        /// </summary>
        SortName,

        /// <summary>
        /// The studios of the item
        /// </summary>
        Studios,

        /// <summary>
        /// The taglines of the item
        /// </summary>
        Taglines,

        /// <summary>
        /// The trailer url of the item
        /// </summary>
        TrailerUrls,

        /// <summary>
        /// The user data of the item
        /// </summary>
        UserData,

        /// <summary>
        /// The media streams
        /// </summary>
        MediaStreams
    }
}
