using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy
{
    /// <summary>
    /// Authorization handler for requiring first time setup or elevated privileges.
    /// </summary>
    public class FirstTimeSetupOrElevatedHandler : AuthorizationHandler<FirstTimeSetupOrElevatedRequirement>
    {
        private readonly IConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstTimeSetupOrElevatedHandler" /> class.
        /// </summary>
        /// <param name="configurationManager">The jellyfin configuration manager.</param>
        public FirstTimeSetupOrElevatedHandler(IConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FirstTimeSetupOrElevatedRequirement firstTimeSetupOrElevatedRequirement)
        {
            if (!_configurationManager.CommonConfiguration.IsStartupWizardCompleted)
            {
                context.Succeed(firstTimeSetupOrElevatedRequirement);
            }
            else if (context.User.IsInRole(UserRoles.Administrator))
            {
                context.Succeed(firstTimeSetupOrElevatedRequirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
