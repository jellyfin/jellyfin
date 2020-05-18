#pragma warning disable CS1591

namespace MediaBrowser.Model.Channels
{
    public enum ChannelFolderType
    {
        /// <summary>
        /// Container
        /// </summary>
        Container = 0,

        /// <summary>
        /// Music album
        /// </summary>
        MusicAlbum = 1,

        /// <summary>
        /// Photo album
        /// </summary>
        PhotoAlbum = 2,

        /// <summary>
        /// Music artist
        /// </summary>
        MusicArtist = 3,

        /// <summary>
        /// Series
        /// </summary>
        Series = 4,

        /// <summary>
        /// Season
        /// </summary>
        Season = 5
    }
}
