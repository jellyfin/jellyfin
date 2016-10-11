namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Used to control the data that gets attached to DtoBaseItems
    /// </summary>
    public enum ItemFields
    {
        /// <summary>
        /// The air time
        /// </summary>
        AirTime,

        /// <summary>
        /// The alternate episode numbers
        /// </summary>
        AlternateEpisodeNumbers,

        /// <summary>
        /// The awards summary
        /// </summary>
        AwardSummary,

        /// <summary>
        /// The budget
        /// </summary>
        Budget,

        /// <summary>
        /// The can delete
        /// </summary>
        CanDelete,

        /// <summary>
        /// The can download
        /// </summary>
        CanDownload,

        /// <summary>
        /// The channel information
        /// </summary>
        ChannelInfo,

        /// <summary>
        /// The chapters
        /// </summary>
        Chapters,

        /// <summary>
        /// The critic rating summary
        /// </summary>
        CriticRatingSummary,

        /// <summary>
        /// The cumulative run time ticks
        /// </summary>
        CumulativeRunTimeTicks,

        /// <summary>
        /// The custom rating
        /// </summary>
        CustomRating,
        
        /// <summary>
        /// The date created of the item
        /// </summary>
        DateCreated,

        /// <summary>
        /// The date last media added
        /// </summary>
        DateLastMediaAdded,

        /// <summary>
        /// Item display preferences
        /// </summary>
        DisplayPreferencesId,

        /// <summary>
        /// The display media type
        /// </summary>
        DisplayMediaType,

        /// <summary>
        /// The etag
        /// </summary>
        Etag,

        /// <summary>
        /// The external urls
        /// </summary>
        ExternalUrls,

        /// <summary>
        /// Genres
        /// </summary>
        Genres,

        /// <summary>
        /// The home page URL
        /// </summary>
        HomePageUrl,

        /// <summary>
        /// The fields that the server supports indexing on
        /// </summary>
        IndexOptions,

        /// <summary>
        /// The item counts
        /// </summary>
        ItemCounts,

        /// <summary>
        /// The keywords
        /// </summary>
        Keywords,

        /// <summary>
        /// The media source count
        /// </summary>
        MediaSourceCount,

        /// <summary>
        /// The media versions
        /// </summary>
        MediaSources,

        /// <summary>
        /// The metascore
        /// </summary>
        Metascore,

        OfficialRatingDescription,

        OriginalTitle,

        /// <summary>
        /// The item overview
        /// </summary>
        Overview,

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
        /// The production locations
        /// </summary>
        ProductionLocations,

        /// <summary>
        /// Imdb, tmdb, etc
        /// </summary>
        ProviderIds,

        /// <summary>
        /// The aspect ratio of the primary image
        /// </summary>
        PrimaryImageAspectRatio,

        /// <summary>
        /// The revenue
        /// </summary>
        Revenue,

        /// <summary>
        /// The season name
        /// </summary>
        SeasonName,

        /// <summary>
        /// The settings
        /// </summary>
        Settings,

        /// <summary>
        /// The short overview
        /// </summary>
        ShortOverview,

        /// <summary>
        /// The screenshot image tags
        /// </summary>
        ScreenshotImageTags,

        /// <summary>
        /// The series genres
        /// </summary>
        SeriesGenres,

        SeriesPrimaryImage,

        /// <summary>
        /// The series studio
        /// </summary>
        SeriesStudio,

        /// <summary>
        /// The sort name of the item
        /// </summary>
        SortName,

        /// <summary>
        /// The special episode numbers
        /// </summary>
        SpecialEpisodeNumbers,

        /// <summary>
        /// The studios of the item
        /// </summary>
        Studios,

        BasicSyncInfo,
        /// <summary>
        /// The synchronize information
        /// </summary>
        SyncInfo,

        /// <summary>
        /// The taglines of the item
        /// </summary>
        Taglines,

        /// <summary>
        /// The tags
        /// </summary>
        Tags,

        /// <summary>
        /// The vote count
        /// </summary>
        VoteCount,

        /// <summary>
        /// The trailer url of the item
        /// </summary>
        RemoteTrailers,

        /// <summary>
        /// The media streams
        /// </summary>
        MediaStreams,

        /// <summary>
        /// The season user data
        /// </summary>
        SeasonUserData,

        /// <summary>
        /// The service name
        /// </summary>
        ServiceName,
        ThemeSongIds,
        ThemeVideoIds
    }
}
