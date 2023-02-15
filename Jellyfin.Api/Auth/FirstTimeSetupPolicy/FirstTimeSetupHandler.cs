using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.FirstTimeSetupPolicy
{
    /// <summary>
    /// Authorization handler for requiring first time setup or default privileges.
    /// </summary>
    public class FirstTimeSetupHandler : AuthorizationHandler<FirstTimeSetupRequirement>
    {
        private readonly IConfigurationManager _configurationManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstTimeSetupHandler" /> class.
        /// </summary>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        public FirstTimeSetupHandler(
            IConfigurationManager configurationManager,
            IUserManager userManager)
        {
            _configurationManager = configurationManager;
            _userManager = userManager;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FirstTimeSetupRequirement requirement)
        {
            if (!_configurationManager.CommonConfiguration.IsStartupWizardCompleted)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (requirement.RequireAdmin && !context.User.IsInRole(UserRoles.Administrator))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (!requirement.ValidateParentalSchedule)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var user = _userManager.GetUserById(context.User.GetUserId());
            if (user is null)
            {
                throw new ResourceNotFoundException();
            }

            if (user.IsParentalScheduleAllowed())
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
