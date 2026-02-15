using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;

namespace Jellyfin.Api.Auth.FirstTimeSetupPolicy
{
    /// <summary>
    /// The authorization requirement, requiring incomplete first time setup or default privileges, for the authorization handler.
    /// </summary>
    public class FirstTimeSetupRequirement : DefaultAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FirstTimeSetupRequirement"/> class.
        /// </summary>
        /// <param name="validateParentalSchedule">A value indicating whether to ignore parental schedule.</param>
        /// <param name="requireAdmin">A value indicating whether administrator role is required.</param>
        public FirstTimeSetupRequirement(bool validateParentalSchedule = false, bool requireAdmin = true) : base(validateParentalSchedule)
        {
            RequireAdmin = requireAdmin;
        }

        /// <summary>
        /// Gets a value indicating whether administrator role is required.
        /// </summary>
        public bool RequireAdmin { get; }
    }
}
