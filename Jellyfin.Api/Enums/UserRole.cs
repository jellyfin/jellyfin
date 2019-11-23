namespace Jellyfin.Api.Enums
{
    /// <summary>
    /// Enum for user roles used in the authentication and authorization for the API.
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// Guest user.
        /// </summary>
        Guest = 0,

        /// <summary>
        /// Regular user with no special privileges.
        /// </summary>
        User = 1,

        /// <summary>
        /// Administrator user with elevated privileges.
        /// </summary>
        Administrator = 2
    }
}
