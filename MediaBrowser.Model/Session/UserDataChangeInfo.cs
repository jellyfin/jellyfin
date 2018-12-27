using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class UserDataChangeInfo
    /// </summary>
    public class UserDataChangeInfo
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user data list.
        /// </summary>
        /// <value>The user data list.</value>
        public UserItemDataDto[] UserDataList { get; set; }
    }
}
