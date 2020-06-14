#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Net
{
    public class NetworkShare
    {
        /// <summary>
        /// The name of the computer that this share belongs to
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Share name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Local path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Share type
        /// </summary>
        public NetworkShareType ShareType { get; set; }

        /// <summary>
        /// Comment
        /// </summary>
        public string Remark { get; set; }
    }
}
