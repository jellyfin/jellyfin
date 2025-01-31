using Jellyfin.Data.Entities;

namespace Jellyfin.Data.Events.Users
{
    /// <summary>
    /// An event that occurs when a user is updated.
    /// </summary>
    public class UserUpdatedEventArgs : GenericEventArgs<User>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The user.</param>
        public UserUpdatedEventArgs(User arg) : base(arg)
        {
        }
    }
}
