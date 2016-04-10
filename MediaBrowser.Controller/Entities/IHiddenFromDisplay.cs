namespace MediaBrowser.Controller.Entities
{
    public interface IHiddenFromDisplay
    {
        /// <summary>
        /// Determines whether the specified user is hidden.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the specified user is hidden; otherwise, <c>false</c>.</returns>
        bool IsHiddenFromUser(User user);
    }
}
