using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.FirstTimeSetupPolicy
{
    /// <summary>
    /// Authorization handler for requiring first time setup or default privileges.
    /// </summary>
    public class FirstTimeSetupHandler : AuthorizationHandler<FirstTimeSetupRequirement>
    {
        private readonly IConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstTimeSetupHandler" /> class.
        /// </summary>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        public FirstTimeSetupHandler(IConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FirstTimeSetupRequirement requirement)
        {
            // Succeed if the startup wizard / first time setup is not complete
            if (!_configurationManager.CommonConfiguration.IsStartupWizardCompleted)
            {
                context.Succeed(requirement);
            }

            // Succeed if user is admin
            else if (context.User.IsInRole(UserRoles.Administrator))
            {
                context.Succeed(requirement);
            }

            // Fail if admin is required and user is not admin
            else if (requirement.RequireAdmin)
            {
                context.Fail();
            }

            // Succeed if admin is not required and user is not guest
            else if (context.User.IsInRole(UserRoles.User))
            {
                context.Succeed(requirement);
            }

            // Any user-specific checks are handled in the DefaultAuthorizationHandler.
            return Task.CompletedTask;
        }
    }
}
