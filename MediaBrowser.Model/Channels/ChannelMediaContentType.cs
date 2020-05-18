#pragma warning disable CS1591

namespace MediaBrowser.Model.Channels
{
    public enum ChannelMediaContentType
    {
        /// <summary>
        /// Clip
        /// </summary>
        Clip = 0,

        /// <summary>
        /// Podcast
        /// </summary>
        Podcast = 1,

        /// <summary>
        /// Trailer
        /// </summary>
        Trailer = 2,

        /// <summary>
        /// Movie
        /// </summary>
        Movie = 3,

        /// <summary>
        /// Episode
        /// </summary>
        Episode = 4,

        /// <summary>
        /// Song
        /// </summary>
        Song = 5,

        /// <summary>
        /// Movie extra
        /// </summary>
        MovieExtra = 6,

        /// <summary>
        /// TV extra
        /// </summary>
        TvExtra = 7
    }
}
