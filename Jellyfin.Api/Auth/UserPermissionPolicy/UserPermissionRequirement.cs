using Jellyfin.Api.Auth.DefaultAuthorizationPolicy;
using Jellyfin.Data.Enums;

namespace Jellyfin.Api.Auth.UserPermissionPolicy
{
    /// <summary>
    /// The user permission requirement.
    /// </summary>
    public class UserPermissionRequirement : DefaultAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserPermissionRequirement"/> class.
        /// </summary>
        /// <param name="requiredPermission">The required <see cref="PermissionKind"/>.</param>
        /// <param name="validateParentalSchedule">Whether to validate the user's parental schedule.</param>
        public UserPermissionRequirement(PermissionKind requiredPermission, bool validateParentalSchedule = true) : base(validateParentalSchedule)
        {
            RequiredPermission = requiredPermission;
        }

        /// <summary>
        /// Gets the required user permission.
        /// </summary>
        public PermissionKind RequiredPermission { get; }
    }
}
