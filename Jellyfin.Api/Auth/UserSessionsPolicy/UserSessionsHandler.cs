using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.UserSessionsPolicy
{
    /// <summary>
    /// Logout any new user sessions if the user is already at the max session limit.
    /// </summary>
    public class UserSessionsHandler : BaseAuthorizationHandler<UserSessionsRequirement>
    {
        private readonly IUserManager _userManager;
        private readonly ISessionManager _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserSessionsHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        public UserSessionsHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor,
            ISessionManager sessionManager)
            : base(userManager, networkManager, httpContextAccessor, sessionManager)
        {
            _userManager = userManager;
            _sessionManager = sessionManager;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserSessionsRequirement requirement)
        {
            var userId = ClaimHelpers.GetUserId(context.User);
            var user = _userManager.GetUserById(userId!.Value);
            var userSessionsCount = _sessionManager.GetSessionCountByUserId(user.Id);
            if (userSessionsCount > user.MaxActiveSessions)
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
