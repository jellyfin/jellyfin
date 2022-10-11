using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SyncPlay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.SyncPlayAccessPolicy
{
    /// <summary>
    /// Default authorization handler.
    /// </summary>
    public class SyncPlayAccessHandler : BaseAuthorizationHandler<SyncPlayAccessRequirement>
    {
        private readonly ISyncPlayManager _syncPlayManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncPlayAccessHandler"/> class.
        /// </summary>
        /// <param name="syncPlayManager">Instance of the <see cref="ISyncPlayManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public SyncPlayAccessHandler(
            ISyncPlayManager syncPlayManager,
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
            : base(userManager, networkManager, httpContextAccessor)
        {
            _syncPlayManager = syncPlayManager;
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

            var userId = context.User.GetUserId();
            var user = _userManager.GetUserById(userId);

            if (requirement.RequiredAccess == SyncPlayAccessRequirementType.HasAccess)
            {
                if (user.SyncPlayAccess == SyncPlayUserAccessType.CreateAndJoinGroups
                    || user.SyncPlayAccess == SyncPlayUserAccessType.JoinGroups
                    || _syncPlayManager.IsUserActive(userId))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else if (requirement.RequiredAccess == SyncPlayAccessRequirementType.CreateGroup)
            {
                if (user.SyncPlayAccess == SyncPlayUserAccessType.CreateAndJoinGroups)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else if (requirement.RequiredAccess == SyncPlayAccessRequirementType.JoinGroup)
            {
                if (user.SyncPlayAccess == SyncPlayUserAccessType.CreateAndJoinGroups
                    || user.SyncPlayAccess == SyncPlayUserAccessType.JoinGroups)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else if (requirement.RequiredAccess == SyncPlayAccessRequirementType.IsInGroup)
            {
                if (_syncPlayManager.IsUserActive(userId))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
