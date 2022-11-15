namespace MediaBrowser.Model.ApiClient
{
    /// <summary>
    /// The server discovery info model.
    /// </summary>
    public class ServerDiscoveryInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerDiscoveryInfo"/> class.
        /// </summary>
        /// <param name="address">The server address.</param>
        /// <param name="id">The server id.</param>
        /// <param name="name">The server name.</param>
        /// <param name="endpointAddress">The endpoint address.</param>
        public ServerDiscoveryInfo(string address, string id, string name, string? endpointAddress = null)
        {
            Address = address;
            Id = id;
            Name = name;
            EndpointAddress = endpointAddress;
        }

        /// <summary>
        /// Gets the address.
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the endpoint address.
        /// </summary>
        public string? EndpointAddress { get; }
    }
}
