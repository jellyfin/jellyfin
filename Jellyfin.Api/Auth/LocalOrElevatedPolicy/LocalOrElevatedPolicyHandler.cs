using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.LocalOrElevatedPolicy
{
    /// <summary>
    /// Authorization handler for requiring a request from the local computer or elevated privileges.
    /// </summary>
    public class LocalOrElevatedPolicyHandler : AuthorizationHandler<LocalOrElevatedPolicyRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalOrElevatedPolicyHandler"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public LocalOrElevatedPolicyHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalOrElevatedPolicyRequirement localOrElevatedPolicyRequirement)
        {
            HttpContext httpContext = _httpContextAccessor.HttpContext;
            if (httpContext.Connection.LocalIpAddress.Equals(httpContext.Connection.RemoteIpAddress))
            {
                context.Succeed(localOrElevatedPolicyRequirement);
            }
            else if (context.User.IsInRole(UserRoles.Administrator))
            {
                context.Succeed(localOrElevatedPolicyRequirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
