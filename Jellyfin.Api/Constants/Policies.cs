namespace Jellyfin.Api.Constants
{
    /// <summary>
    /// Policies for the API authorization.
    /// </summary>
    public static class Policies
    {
        /// <summary>
        /// Policy name for requiring first time setup or elevated privileges.
        /// </summary>
        public const string FirstTimeSetupOrElevated = "FirstTimeOrElevated";

        /// <summary>
        /// Policy name for requiring elevated privileges.
        /// </summary>
        public const string RequiresElevation = "RequiresElevation";
    }
}
