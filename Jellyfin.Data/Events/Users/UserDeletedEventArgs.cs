using Jellyfin.Data.Entities;

namespace Jellyfin.Data.Events.Users
{
    /// <summary>
    /// An event that occurs when a user is deleted.
    /// </summary>
    public class UserDeletedEventArgs : GenericEventArgs<User>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserDeletedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The user.</param>
        public UserDeletedEventArgs(User arg) : base(arg)
        {
        }
    }
}
