namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// The base item kind.
    /// </summary>
    /// <remarks>
    /// This enum is generated from all classes that inherit from <c>BaseItem</c>.
    /// You may leave it sorted but make sure to add new ids after Final.
    /// </remarks>
    public enum BaseItemKind
    {
        /// <summary>
        /// Item is aggregate folder.
        /// </summary>
        AggregateFolder = 0,

        /// <summary>
        /// Item is audio.
        /// </summary>
        Audio = 1,

        /// <summary>
        /// Item is audio book.
        /// </summary>
        AudioBook = 2,

        /// <summary>
        /// Item is base plugin folder.
        /// </summary>
        BasePluginFolder = 3,

        /// <summary>
        /// Item is book.
        /// </summary>
        Book = 4,

        /// <summary>
        /// Item is box set.
        /// </summary>
        BoxSet = 5,

        /// <summary>
        /// Item is channel.
        /// </summary>
        Channel = 6,

        /// <summary>
        /// Item is channel folder item.
        /// </summary>
        ChannelFolderItem = 7,

        /// <summary>
        /// Item is collection folder.
        /// </summary>
        CollectionFolder = 8,

        /// <summary>
        /// Item is episode.
        /// </summary>
        Episode = 9,

        /// <summary>
        /// Item is folder.
        /// </summary>
        Folder = 10,

        /// <summary>
        /// Item is genre.
        /// </summary>
        Genre = 11,

        /// <summary>
        /// Item is a live tv channel.
        /// </summary>
        LiveTvChannel = 12,

        /// <summary>
        /// Item is a live tv program.
        /// </summary>
        LiveTvProgram = 13,

        /// <summary>
        /// Item is manual playlists folder.
        /// </summary>
        ManualPlaylistsFolder = 14,

        /// <summary>
        /// Item is movie.
        /// </summary>
        Movie = 15,

        /// <summary>
        /// Item is music album.
        /// </summary>
        MusicAlbum = 16,

        /// <summary>
        /// Item is music artist.
        /// </summary>
        MusicArtist = 17,

        /// <summary>
        /// Item is music genre.
        /// </summary>
        MusicGenre = 18,

        /// <summary>
        /// Item is music video.
        /// </summary>
        MusicVideo = 19,

        /// <summary>
        /// Item is person.
        /// </summary>
        Person = 20,

        /// <summary>
        /// Item is photo.
        /// </summary>
        Photo = 21,

        /// <summary>
        /// Item is photo album.
        /// </summary>
        PhotoAlbum = 22,

        /// <summary>
        /// Item is playlist.
        /// </summary>
        Playlist = 23,

        /// <summary>
        /// Item is recording.
        /// </summary>
        /// <remarks>
        /// Manually added.
        /// </remarks>
        Recording = 24,

        /// <summary>
        /// Item is season.
        /// </summary>
        Season = 25,

        /// <summary>
        /// Item is series.
        /// </summary>
        Series = 26,

        /// <summary>
        /// Item is studio.
        /// </summary>
        Studio = 27,

        /// <summary>
        /// Item is trailer.
        /// </summary>
        Trailer = 28,

        /// <summary>
        /// Item is user root folder.
        /// </summary>
        UserRootFolder = 29,

        /// <summary>
        /// Item is user view.
        /// </summary>
        UserView = 30,

        /// <summary>
        /// Item is video.
        /// </summary>
        Video = 31,

        /// <summary>
        /// Item is year.
        /// </summary>
        Year = 32,

        /// <summary>
        /// Item is final.
        /// Not used. Just a reference point to new ids.
        /// Not a subject of DB.
        /// </summary>
        Final = 33
    }
}
