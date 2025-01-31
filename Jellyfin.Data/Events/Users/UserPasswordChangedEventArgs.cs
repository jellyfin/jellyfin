using Jellyfin.Data.Entities;

namespace Jellyfin.Data.Events.Users
{
    /// <summary>
    /// An event that occurs when a user's password has changed.
    /// </summary>
    public class UserPasswordChangedEventArgs : GenericEventArgs<User>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserPasswordChangedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The user.</param>
        public UserPasswordChangedEventArgs(User arg) : base(arg)
        {
        }
    }
}
