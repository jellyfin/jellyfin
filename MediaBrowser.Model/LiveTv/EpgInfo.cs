using System;

namespace MediaBrowser.Model.LiveTv
{
    public class EpgInfo
    {
        /// <summary>
        /// Id of the program.
        /// </summary>
        public string Id { get; set; }

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