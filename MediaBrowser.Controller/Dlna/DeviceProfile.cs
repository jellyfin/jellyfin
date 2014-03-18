
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
        /// Gets or sets the transcoding profiles.
        /// </summary>
        /// <value>The transcoding profiles.</value>
        public TranscodingProfile[] TranscodingProfiles { get; set; }
        
        /// <summary>
        /// Gets or sets the direct play profiles.
        /// </summary>
        /// <value>The direct play profiles.</value>
        public DirectPlayProfile[] DirectPlayProfiles { get; set; }

        /// <summary>
        /// Gets or sets the identification.
        /// </summary>
        /// <value>The identification.</value>
        public DeviceIdentification Identification { get; set; }

        public string FriendlyName { get; set; }
        public string Manufacturer { get; set; }
        public string ManufacturerUrl { get; set; }
        public string ModelName { get; set; }
        public string ModelDescription { get; set; }
        public string ModelNumber { get; set; }
        public string ModelUrl { get; set; }
        /// <summary>
        /// Controls the content of the X_DLNADOC element in the urn:schemas-dlna-org:device-1-0 namespace.
        /// </summary>
        public string XDlnaDoc { get; set; }
        /// <summary>
        /// Controls the content of the X_DLNACAP element in the urn:schemas-dlna-org:device-1-0 namespace.
        /// </summary>
        public string XDlnaCap { get; set; }
        /// <summary>
        /// Controls the content of the aggregationFlags element in the urn:schemas-sonycom:av.
        /// </summary>
        public string SonyAggregationFlags { get; set; }

        public string ProtocolInfo { get; set; }

        public DeviceProfile()
        {
            DirectPlayProfiles = new DirectPlayProfile[] { };
            TranscodingProfiles = new TranscodingProfile[] { };
        }
    }
}
