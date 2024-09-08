using Jellyfin.Data.Entities;

namespace Jellyfin.Data.Events.Users
{
    /// <summary>
    /// An event that occurs when a user is created.
    /// </summary>
    public class UserCreatedEventArgs : GenericEventArgs<User>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserCreatedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The user.</param>
        public UserCreatedEventArgs(User arg) : base(arg)
        {
        }
    }
}
