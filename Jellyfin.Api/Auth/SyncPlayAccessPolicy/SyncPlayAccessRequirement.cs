using Jellyfin.Data.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.SyncPlayAccessPolicy
{
    /// <summary>
    /// The default authorization requirement.
    /// </summary>
    public class SyncPlayAccessRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayAccessRequirement"/> class.
        /// </summary>
        /// <param name="requiredAccess">A value of <see cref="SyncPlayAccessRequirementType"/>.</param>
        public SyncPlayAccessRequirement(SyncPlayAccessRequirementType requiredAccess)
        {
            RequiredAccess = requiredAccess;
        }

        /// <summary>
        /// Gets the required SyncPlay access.
        /// </summary>
        public SyncPlayAccessRequirementType RequiredAccess { get; }
    }
}
