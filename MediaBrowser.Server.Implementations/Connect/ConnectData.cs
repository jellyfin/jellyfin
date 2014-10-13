using MediaBrowser.Model.Connect;
using System.Collections.Generic;

namespace MediaBrowser.Server.Implementations.Connect
{
    public class ConnectData
    {
        /// <summary>
        /// Gets or sets the server identifier.
        /// </summary>
        /// <value>The server identifier.</value>
        public string ServerId { get; set; }
        /// <summary>
        /// Gets or sets the access key.
        /// </summary>
        /// <value>The access key.</value>
        public string AccessKey { get; set; }

        /// <summary>
        /// Gets or sets the authorizations.
        /// </summary>
        /// <value>The authorizations.</value>
        public List<ConnectAuthorization> PendingAuthorizations { get; set; }

        public ConnectData()
        {
            PendingAuthorizations = new List<ConnectAuthorization>();
        }
    }
}
