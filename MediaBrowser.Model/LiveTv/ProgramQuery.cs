namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ProgramQuery.
    /// </summary>
    public class ProgramQuery
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string[] ChannelIdList { get; set; }

        public ProgramQuery()
        {
            ChannelIdList = new string[] { };
        }
    }
}
