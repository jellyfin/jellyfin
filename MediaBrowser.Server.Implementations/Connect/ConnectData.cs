using System;
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
        public List<ConnectAuthorization> Authorizations { get; set; }

        public ConnectData()
        {
            Authorizations = new List<ConnectAuthorization>();
        }
    }

    public class ConnectAuthorization
    {
        public string LocalUserId { get; set; }
        public string AccessToken { get; set; }

        public ConnectAuthorization()
        {
            AccessToken = new Guid().ToString("N");
        }
    }
}
