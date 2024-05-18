using System.Threading.Tasks;
using Jellyfin.Api.Constants;
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
            if (!_configurationManager.CommonConfiguration.IsStartupWizardCompleted)
            {
                context.Succeed(requirement);
            }
            else if (requirement.RequireAdmin && !context.User.IsInRole(UserRoles.Administrator))
            {
                context.Fail();
            }
            else if (!requirement.RequireAdmin && context.User.IsInRole(UserRoles.Guest))
            {
                context.Fail();
            }
            else
            {
                // Any user-specific checks are handled in the DefaultAuthorizationHandler.
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
