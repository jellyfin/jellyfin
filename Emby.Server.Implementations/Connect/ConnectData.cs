using System;
using System.Collections.Generic;

namespace Emby.Server.Implementations.Connect
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
        public List<ConnectAuthorizationInternal> PendingAuthorizations { get; set; }

        /// <summary>
        /// Gets or sets the last authorizations refresh.
        /// </summary>
        /// <value>The last authorizations refresh.</value>
        public DateTime LastAuthorizationsRefresh { get; set; }

        public ConnectData()
        {
            PendingAuthorizations = new List<ConnectAuthorizationInternal>();
        }
    }
}
