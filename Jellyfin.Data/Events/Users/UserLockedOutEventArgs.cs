using Jellyfin.Data.Entities;

namespace Jellyfin.Data.Events.Users
{
    /// <summary>
    /// An event that occurs when a user is locked out.
    /// </summary>
    public class UserLockedOutEventArgs : GenericEventArgs<User>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserLockedOutEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The user.</param>
        public UserLockedOutEventArgs(User arg) : base(arg)
        {
        }
    }
}
