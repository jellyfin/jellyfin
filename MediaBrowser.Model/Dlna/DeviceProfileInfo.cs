namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DeviceProfileInfo" />.
    /// </summary>
    public class DeviceProfileInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceProfileInfo"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        /// <param name="type">The <see cref="DeviceProfileType"/>.</param>
        public DeviceProfileInfo(string id, string name, DeviceProfileType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets the Id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the Name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the Type.
        /// </summary>
        public DeviceProfileType Type { get; }
    }
}
