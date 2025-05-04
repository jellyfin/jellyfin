using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.DefaultAuthorizationPolicy
{
    /// <summary>
    /// The default authorization requirement.
    /// </summary>
    public class DefaultAuthorizationRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAuthorizationRequirement"/> class.
        /// </summary>
        /// <param name="validateParentalSchedule">A value indicating whether to validate parental schedule.</param>
        public DefaultAuthorizationRequirement(bool validateParentalSchedule = true)
        {
            ValidateParentalSchedule = validateParentalSchedule;
        }

        /// <summary>
        /// Gets a value indicating whether to ignore parental schedule.
        /// </summary>
        public bool ValidateParentalSchedule { get; }
    }
}
