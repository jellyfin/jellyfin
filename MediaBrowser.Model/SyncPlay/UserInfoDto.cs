using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class UserInfoDto.
    /// </summary>
    public class UserInfoDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserInfoDto"/> class.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="userName">The user name.</param>
        public UserInfoDto(Guid userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }

        /// <summary>
        /// Gets the user's identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId { get; }

        /// <summary>
        /// Gets the user's name.
        /// </summary>
        /// <value>The user name.</value>
        public string UserName { get; }
    }
}
