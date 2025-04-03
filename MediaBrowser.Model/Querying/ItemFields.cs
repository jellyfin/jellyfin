namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Used to control the data that gets attached to DtoBaseItems.
    /// </summary>
    public enum ItemFields
    {
        /// <summary>
        /// The air time.
        /// </summary>
        AirTime,

        /// <summary>
        /// The can delete.
        /// </summary>
        CanDelete,

        /// <summary>
        /// The can download.
        /// </summary>
        CanDownload,

        /// <summary>
        /// The channel information.
        /// </summary>
        ChannelInfo,

        /// <summary>
        /// The chapters.
        /// </summary>
        Chapters,

        /// <summary>
        /// The trickplay manifest.
        /// </summary>
        Trickplay,

        /// <summary>
        /// The child count.
        /// </summary>
        ChildCount,

        /// <summary>
        /// The cumulative run time ticks.
        /// </summary>
        CumulativeRunTimeTicks,

        /// <summary>
        /// The custom rating.
        /// </summary>
        CustomRating,

        /// <summary>
        /// The date created of the item.
        /// </summary>
        DateCreated,

        /// <summary>
        /// The date last media added.
        /// </summary>
        DateLastMediaAdded,

        /// <summary>
        /// Item display preferences.
        /// </summary>
        DisplayPreferencesId,

        /// <summary>
        /// The etag.
        /// </summary>
        Etag,

        /// <summary>
        /// The external urls.
        /// </summary>
        ExternalUrls,

        /// <summary>
        /// Genres.
        /// </summary>
        Genres,

        /// <summary>
        /// The item counts.
        /// </summary>
        ItemCounts,

        /// <summary>
        /// The media source count.
        /// </summary>
        MediaSourceCount,

        /// <summary>
        /// The media versions.
        /// </summary>
        MediaSources,

        /// <summary>
        /// The original title.
        /// </summary>
        OriginalTitle,

        /// <summary>
        /// The item overview.
        /// </summary>
        Overview,

        /// <summary>
        /// The id of the item's parent.
        /// </summary>
        ParentId,

        /// <summary>
        /// The physical path of the item.
        /// </summary>
        Path,

        /// <summary>
        /// The list of people for the item.
        /// </summary>
        People,

        /// <summary>
        /// Value indicating whether playback access is granted.
        /// </summary>
        PlayAccess,

        /// <summary>
        /// The production locations.
        /// </summary>
        ProductionLocations,

        /// <summary>
        /// The ids from IMDb, TMDb, etc.
        /// </summary>
        ProviderIds,

        /// <summary>
        /// The aspect ratio of the primary image.
        /// </summary>
        PrimaryImageAspectRatio,

        /// <summary>
        /// The recursive item count.
        /// </summary>
        RecursiveItemCount,

        /// <summary>
        /// The settings.
        /// </summary>
        Settings,

        /// <summary>
        /// The series studio.
        /// </summary>
        SeriesStudio,

        /// <summary>
        /// The sort name of the item.
        /// </summary>
        SortName,

        /// <summary>
        /// The special episode numbers.
        /// </summary>
        SpecialEpisodeNumbers,

        /// <summary>
        /// The studios of the item.
        /// </summary>
        Studios,

        /// <summary>
        /// The taglines of the item.
        /// </summary>
        Taglines,

        /// <summary>
        /// The tags.
        /// </summary>
        Tags,

        /// <summary>
        /// The trailer url of the item.
        /// </summary>
        RemoteTrailers,

        /// <summary>
        /// The media streams.
        /// </summary>
        MediaStreams,

        /// <summary>
        /// The season user data.
        /// </summary>
        SeasonUserData,

        /// <summary>
        /// The last time metadata was refreshed.
        /// </summary>
        DateLastRefreshed,

        /// <summary>
        /// The last time metadata was saved.
        /// </summary>
        DateLastSaved,

        /// <summary>
        /// The refresh state.
        /// </summary>
        RefreshState,

        /// <summary>
        /// The channel image.
        /// </summary>
        ChannelImage,

        /// <summary>
        /// Value indicating whether media source display is enabled.
        /// </summary>
        EnableMediaSourceDisplay,

        /// <summary>
        /// The width.
        /// </summary>
        Width,

        /// <summary>
        /// The height.
        /// </summary>
        Height,

        /// <summary>
        /// The external Ids.
        /// </summary>
        ExtraIds,

        /// <summary>
        /// The local trailer count.
        /// </summary>
        LocalTrailerCount,

        /// <summary>
        /// Value indicating whether the item is HD.
        /// </summary>
        IsHD,

        /// <summary>
        /// The special feature count.
        /// </summary>
        SpecialFeatureCount
    }
}
