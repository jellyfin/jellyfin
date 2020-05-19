namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// The types of user preferences.
    /// </summary>
    public enum PreferenceKind
    {
        /// <summary>
        /// A list of blocked tags.
        /// </summary>
        BlockedTags,

        /// <summary>
        /// A list of blocked channels.
        /// </summary>
        BlockedChannels,

        /// <summary>
        /// A list of blocked media folders.
        /// </summary>
        BlockedMediaFolders,

        /// <summary>
        /// A list of enabled devices.
        /// </summary>
        EnabledDevices,

        /// <summary>
        /// A list of enabled channels
        /// </summary>
        EnabledChannels,

        /// <summary>
        /// A list of enabled folders.
        /// </summary>
        EnabledFolders,

        /// <summary>
        /// A list of folders to allow content deletion from.
        /// </summary>
        EnableContentDeletionFromFolders,

        /// <summary>
        /// A list of latest items to exclude.
        /// </summary>
        LatestItemExcludes,

        /// <summary>
        /// A list of media to exclude.
        /// </summary>
        MyMediaExcludes,

        /// <summary>
        /// A list of grouped folders.
        /// </summary>
        GroupedFolders,

        /// <summary>
        /// A list of unrated items to block.
        /// </summary>
        BlockUnratedItems,

        /// <summary>
        /// A list of ordered views.
        /// </summary>
        OrderedViews
    }
}
