using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.UserPermissionPolicy
{
    /// <summary>
    /// User permission authorization handler.
    /// </summary>
    public class UserPermissionHandler : AuthorizationHandler<UserPermissionRequirement>
    {
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserPermissionHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        public UserPermissionHandler(IUserManager userManager)
        {
            _userManager = userManager;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserPermissionRequirement requirement)
        {
            // Api keys have global permissions, so just succeed the requirement.
            if (context.User.GetIsApiKey())
            {
                context.Succeed(requirement);
            }
            else
            {
                var userId = context.User.GetUserId();
                if (!userId.IsEmpty())
                {
                    var user = _userManager.GetUserById(context.User.GetUserId());
                    if (user is null)
                    {
                        throw new ResourceNotFoundException();
                    }

                    if (user.HasPermission(requirement.RequiredPermission))
                    {
                        context.Succeed(requirement);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
