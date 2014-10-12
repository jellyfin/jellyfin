using System;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Devices
{
    public class DeviceInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the last name of the user.
        /// </summary>
        /// <value>The last name of the user.</value>
        public string LastUserName { get; set; }
        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>The name of the application.</value>
        public string AppName { get; set; }
        /// <summary>
        /// Gets or sets the last user identifier.
        /// </summary>
        /// <value>The last user identifier.</value>
        public string LastUserId { get; set; }
        /// <summary>
        /// Gets or sets the date last modified.
        /// </summary>
        /// <value>The date last modified.</value>
        public DateTime DateLastModified { get; set; }
        /// <summary>
        /// Gets or sets the capabilities.
        /// </summary>
        /// <value>The capabilities.</value>
        public ClientCapabilities Capabilities { get; set; }

        public DeviceInfo()
        {
            Capabilities = new ClientCapabilities();
        }
    }
}
