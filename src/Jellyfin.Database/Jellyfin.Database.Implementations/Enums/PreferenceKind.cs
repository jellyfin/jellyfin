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
        BlockedTags = 0,

        /// <summary>
        /// A list of blocked channels.
        /// </summary>
        BlockedChannels = 1,

        /// <summary>
        /// A list of blocked media folders.
        /// </summary>
        BlockedMediaFolders = 2,

        /// <summary>
        /// A list of enabled devices.
        /// </summary>
        EnabledDevices = 3,

        /// <summary>
        /// A list of enabled channels.
        /// </summary>
        EnabledChannels = 4,

        /// <summary>
        /// A list of enabled folders.
        /// </summary>
        EnabledFolders = 5,

        /// <summary>
        /// A list of folders to allow content deletion from.
        /// </summary>
        EnableContentDeletionFromFolders = 6,

        /// <summary>
        /// A list of latest items to exclude.
        /// </summary>
        LatestItemExcludes = 7,

        /// <summary>
        /// A list of media to exclude.
        /// </summary>
        MyMediaExcludes = 8,

        /// <summary>
        /// A list of grouped folders.
        /// </summary>
        GroupedFolders = 9,

        /// <summary>
        /// A list of unrated items to block.
        /// </summary>
        BlockUnratedItems = 10,

        /// <summary>
        /// A list of ordered views.
        /// </summary>
        OrderedViews = 11,

        /// <summary>
        /// A list of allowed tags.
        /// </summary>
        AllowedTags = 12
    }
}
