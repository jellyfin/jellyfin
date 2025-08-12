namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// IUserValidation.
    /// </summary>
    public interface IUserValidation
    {
        /// <summary>
        /// Checks if the user's username is valid.
        /// </summary>
        /// <param name="name">The user's username.</param>
        void ThrowIfInvalidUsername(string name);

        /// <summary>
        /// Determines whether the specified username is valid.
        /// </summary>
        /// <param name="name">The username to validate.</param>
        /// <returns>true if the username is valid; otherwise, false.</returns>
        bool IsValidUsername(string name);
    }
}
