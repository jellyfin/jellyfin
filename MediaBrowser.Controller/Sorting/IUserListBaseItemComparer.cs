#nullable disable

using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Sorting
{
    /// <summary>
    /// Represents a BaseItem comparer that requires a user list manager to perform its comparison.
    /// </summary>
    public interface IUserListBaseItemComparer : IUserBaseItemComparer
    {
        /// <summary>
        /// Gets or sets the user list manager.
        /// </summary>
        /// <value>The user list manager.</value>
        IUserListManager UserListManager { get; set; }
    }
}
