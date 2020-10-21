#pragma warning disable CS1591

namespace MediaBrowser.Model.Channels
{
    public enum ChannelMediaContentType
    {
        /// <summary>
        /// Clip channel media content type
        /// </summary>
        Clip = 0,

        /// <summary>
        /// Podcast channel media content type
        /// </summary>
        Podcast = 1,

        /// <summary>
        /// Trailer channel media content type
        /// </summary>
        Trailer = 2,

        /// <summary>
        /// Movie channel media content type
        /// </summary>
        Movie = 3,

        /// <summary>
        /// Episode channel media content type
        /// </summary>
        Episode = 4,

        /// <summary>
        /// Song channel media content type
        /// </summary>
        Song = 5,

        /// <summary>
        /// Movie extra channel media content type
        /// </summary>
        MovieExtra = 6,

        /// <summary>
        /// Tv extra channel media content type
        /// </summary>
        TvExtra = 7
    }
}
