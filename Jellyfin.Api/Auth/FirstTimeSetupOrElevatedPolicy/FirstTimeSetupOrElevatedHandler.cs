using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy
{
    public class FirstTimeSetupOrElevatedHandler : AuthorizationHandler<FirstTimeSetupOrElevatedRequirement>
    {
        private readonly IConfigurationManager _configurationManager;

        public FirstTimeSetupOrElevatedHandler(IConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        protected override  Task HandleRequirementAsync(AuthorizationHandlerContext context, FirstTimeSetupOrElevatedRequirement firstTimeSetupOrElevatedRequirement)
        {
            if (!_configurationManager.CommonConfiguration.IsStartupWizardCompleted)
            {
                context.Succeed(firstTimeSetupOrElevatedRequirement);
            }
            else if (context.User.IsInRole("Administrator"))
            {
                // TODO user role enum
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
