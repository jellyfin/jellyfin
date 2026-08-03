namespace MediaBrowser.Controller.Channels
{
    /// <summary>
    /// The parental rating of a channel.
    /// </summary>
    public enum ChannelParentalRating
    {
        /// <summary>
        /// Suitable for a general audience.
        /// </summary>
        GeneralAudience = 0,

        /// <summary>
        /// Parental guidance suggested (US PG).
        /// </summary>
        UsPG = 1,

        /// <summary>
        /// Parents strongly cautioned (US PG-13).
        /// </summary>
        UsPG13 = 2,

        /// <summary>
        /// Restricted (US R).
        /// </summary>
        UsR = 3,

        /// <summary>
        /// Suitable for adults only.
        /// </summary>
        Adult = 4
    }
}
