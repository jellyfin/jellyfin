#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public class DeviceProfileInfo
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public DeviceProfileType Type { get; set; }
    }
}
