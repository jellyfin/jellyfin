#pragma warning disable CS1591

namespace MediaBrowser.Model.Channels
{
    public enum ChannelFolderType
    {
        /// <summary>
        /// Abstract channel folder type
        /// </summary>
        Container = 0,

        /// <summary>
        /// Music album channel folder type
        /// </summary>
        MusicAlbum = 1,

        /// <summary>
        /// Photo album channel folder type
        /// </summary>
        PhotoAlbum = 2,

        /// <summary>
        /// Music artist channel folder type
        /// </summary>
        MusicArtist = 3,

        /// <summary>
        /// Series channel folder type
        /// </summary>
        Series = 4,

        /// <summary>
        /// Series Season album channel folder type
        /// </summary>
        Season = 5
    }
}
