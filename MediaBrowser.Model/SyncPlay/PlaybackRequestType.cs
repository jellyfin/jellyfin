namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum PlaybackRequestType.
    /// </summary>
    public enum PlaybackRequestType
    {
        /// <summary>
        /// A user is setting a new playlist.
        /// </summary>
        Play = 0,

        /// <summary>
        /// A user is changing the playlist item.
        /// </summary>
        SetPlaylistItem = 1,

        /// <summary>
        /// A user is removing items from the playlist.
        /// </summary>
        RemoveFromPlaylist = 2,

        /// <summary>
        /// A user is moving an item in the playlist.
        /// </summary>
        MovePlaylistItem = 3,

        /// <summary>
        /// A user is adding items to the playlist.
        /// </summary>
        Queue = 4,

        /// <summary>
        /// A user is requesting an unpause command for the group.
        /// </summary>
        Unpause = 5,

        /// <summary>
        /// A user is requesting a pause command for the group.
        /// </summary>
        Pause = 6,

        /// <summary>
        /// A user is requesting a stop command for the group.
        /// </summary>
        Stop = 7,

        /// <summary>
        /// A user is requesting a seek command for the group.
        /// </summary>
        Seek = 8,

         /// <summary>
        /// A user is signaling that playback is buffering.
        /// </summary>
        Buffer = 9,

        /// <summary>
        /// A user is signaling that playback resumed.
        /// </summary>
        Ready = 10,

        /// <summary>
        /// A user is requesting next item in playlist.
        /// </summary>
        NextItem = 11,

        /// <summary>
        /// A user is requesting previous item in playlist.
        /// </summary>
        PreviousItem = 12,

        /// <summary>
        /// A user is setting the repeat mode.
        /// </summary>
        SetRepeatMode = 13,

        /// <summary>
        /// A user is setting the shuffle mode.
        /// </summary>
        SetShuffleMode = 14,

        /// <summary>
        /// A user is reporting their ping.
        /// </summary>
        Ping = 15,

        /// <summary>
        /// A user is requesting to be ignored on group wait.
        /// </summary>
        IgnoreWait = 16
    }
}
