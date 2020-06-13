#nullable disable
using System;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class SessionUserInfo.
    /// </summary>
    public class SessionUserInfo
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>The name of the user.</value>
        public string UserName { get; set; }
    }
}
