using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.SyncPlayAccessPolicy
{
    /// <summary>
    /// Default authorization handler.
    /// </summary>
    public class SyncPlayAccessHandler : BaseAuthorizationHandler<SyncPlayAccessRequirement>
    {
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayAccessHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public SyncPlayAccessHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
            : base(userManager, networkManager, httpContextAccessor)
        {
            _userManager = userManager;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SyncPlayAccessRequirement requirement)
        {
            if (!ValidateClaims(context.User))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var userId = ClaimHelpers.GetUserId(context.User);
            var user = _userManager.GetUserById(userId!.Value);

            if ((requirement.RequiredAccess.HasValue && user.SyncPlayAccess == requirement.RequiredAccess)
                || user.SyncPlayAccess == SyncPlayAccess.CreateAndJoinGroups)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
