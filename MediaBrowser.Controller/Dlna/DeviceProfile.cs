
namespace MediaBrowser.Controller.Dlna
{
    public class DeviceProfile
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the client.
        /// </summary>
        /// <value>The type of the client.</value>
        public string ClientType { get; set; }

        /// <summary>
        /// Gets or sets the name of the friendly.
        /// </summary>
        /// <value>The name of the friendly.</value>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the model number.
        /// </summary>
        /// <value>The model number.</value>
        public string ModelNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the model.
        /// </summary>
        /// <value>The name of the model.</value>
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets the transcoding profiles.
        /// </summary>
        /// <value>The transcoding profiles.</value>
        public TranscodingProfile[] TranscodingProfiles { get; set; }
        
        /// <summary>
        /// Gets or sets the direct play profiles.
        /// </summary>
        /// <value>The direct play profiles.</value>
        public DirectPlayProfile[] DirectPlayProfiles { get; set; }

        public DeviceProfile()
        {
            DirectPlayProfiles = new DirectPlayProfile[] { };
            TranscodingProfiles = new TranscodingProfile[] { };
        }
    }
}
