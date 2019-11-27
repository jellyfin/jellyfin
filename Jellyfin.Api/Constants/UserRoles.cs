namespace Jellyfin.Api.Constants
{
    /// <summary>
    /// Constants for user roles used in the authentication and authorization for the API.
    /// </summary>
    public static class UserRoles
    {
        /// <summary>
        /// Guest user.
        /// </summary>
        public const string Guest = "Guest";

        /// <summary>
        /// Regular user with no special privileges.
        /// </summary>
        public const string User = "User";

        /// <summary>
        /// Administrator user with elevated privileges.
        /// </summary>
        public const string Administrator = "Administrator";
    }
}
