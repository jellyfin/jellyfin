using System;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class UserDataChangeInfo.
    /// </summary>
    public class UserDataChangeInfo
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the user data list.
        /// </summary>
        /// <value>The user data list.</value>
        public required UserItemDataDto[] UserDataList { get; set; }
    }
}
