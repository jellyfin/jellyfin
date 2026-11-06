namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// The type of extra media associated with an item.
    /// </summary>
    public enum ExtraType
    {
        /// <summary>
        /// An unknown extra type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A short clip.
        /// </summary>
        Clip = 1,

        /// <summary>
        /// A trailer.
        /// </summary>
        Trailer = 2,

        /// <summary>
        /// A behind-the-scenes video.
        /// </summary>
        BehindTheScenes = 3,

        /// <summary>
        /// A deleted scene.
        /// </summary>
        DeletedScene = 4,

        /// <summary>
        /// An interview.
        /// </summary>
        Interview = 5,

        /// <summary>
        /// A scene.
        /// </summary>
        Scene = 6,

        /// <summary>
        /// A sample video.
        /// </summary>
        Sample = 7,

        /// <summary>
        /// A theme song.
        /// </summary>
        ThemeSong = 8,

        /// <summary>
        /// A theme video.
        /// </summary>
        ThemeVideo = 9,

        /// <summary>
        /// A featurette.
        /// </summary>
        Featurette = 10,

        /// <summary>
        /// A short film.
        /// </summary>
        Short = 11
    }
}
