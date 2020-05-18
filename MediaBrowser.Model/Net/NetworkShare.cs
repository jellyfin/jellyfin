#pragma warning disable CS1591

namespace MediaBrowser.Model.Net
{
    public class NetworkShare
    {
        /// <summary>
        /// Gets or sets the name of the computer that this share belongs to.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the share name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the local path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the share type.
        /// </summary>
        public NetworkShareType ShareType { get; set; }

        /// <summary>
        /// Gets or sets the remark.
        /// </summary>
        public string Remark { get; set; }
    }
}
