using System;

namespace MediaBrowser.Model.LiveTv
{
    public class ProgramInfo
    {
        /// <summary>
        /// Id of the program.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Gets or sets the external channel identifier.
        /// </summary>
        /// <value>The external channel identifier.</value>
        public string ExternalChannelId { get; set; }
        
        /// <summary>
        /// Name of the program
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the progam.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The start date of the program, in UTC.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end date of the program, in UTC.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Genre of the program.
        /// </summary>
        public string Genre { get; set; }
    }
}