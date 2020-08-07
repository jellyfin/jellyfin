namespace Jellyfin.Api.Constants
{
    /// <summary>
    /// Policies for the API authorization.
    /// </summary>
    public static class Policies
    {
        /// <summary>
        /// Policy name for default authorization.
        /// </summary>
        public const string DefaultAuthorization = "DefaultAuthorization";

        /// <summary>
        /// Policy name for requiring first time setup or elevated privileges.
        /// </summary>
        public const string FirstTimeSetupOrElevated = "FirstTimeSetupOrElevated";

        /// <summary>
        /// Policy name for requiring elevated privileges.
        /// </summary>
        public const string RequiresElevation = "RequiresElevation";

        /// <summary>
        /// Policy name for allowing local access only.
        /// </summary>
        public const string LocalAccessOnly = "LocalAccessOnly";

        /// <summary>
        /// Policy name for escaping schedule controls.
        /// </summary>
        public const string IgnoreParentalControl = "IgnoreParentalControl";

        /// <summary>
        /// Policy name for requiring download permission.
        /// </summary>
        public const string Download = "Download";

        /// <summary>
        /// Policy name for requiring first time setup or default permissions.
        /// </summary>
        public const string FirstTimeSetupOrDefault = "FirstTimeSetupOrDefault";

        /// <summary>
        /// Policy name for requiring local access or elevated privileges.
        /// </summary>
        public const string LocalAccessOrRequiresElevation = "LocalAccessOrRequiresElevation";

        /// <summary>
        /// Policy name for escaping schedule controls or requiring first time setup.
        /// </summary>
        public const string FirstTimeSetupOrIgnoreParentalControl = "FirstTimeSetupOrIgnoreParentalControl";
    }
}
