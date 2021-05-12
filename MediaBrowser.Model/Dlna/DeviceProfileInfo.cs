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
        /// <param name="id">The Id of the profile.</param>
        /// <param name="name">The name of the profile.</param>
        /// <param name="type">The type of the profile.</param>
        public DeviceProfileInfo(string id, string name, DeviceProfileType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        public DeviceProfileType Type { get; }
    }
}
