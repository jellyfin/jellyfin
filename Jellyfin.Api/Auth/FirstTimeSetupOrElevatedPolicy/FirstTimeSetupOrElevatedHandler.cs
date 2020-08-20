using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.FirstTimeSetupOrElevatedPolicy
{
    /// <summary>
    /// Authorization handler for requiring first time setup or elevated privileges.
    /// </summary>
    public class FirstTimeSetupOrElevatedHandler : BaseAuthorizationHandler<FirstTimeSetupOrElevatedRequirement>
    {
        private readonly IConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirstTimeSetupOrElevatedHandler" /> class.
        /// </summary>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        public FirstTimeSetupOrElevatedHandler(
            IConfigurationManager configurationManager,
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor)
            : base(userManager, networkManager, httpContextAccessor)
        {
            _configurationManager = configurationManager;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FirstTimeSetupOrElevatedRequirement firstTimeSetupOrElevatedRequirement)
        {
            if (!_configurationManager.CommonConfiguration.IsStartupWizardCompleted)
            {
                context.Succeed(firstTimeSetupOrElevatedRequirement);
                return Task.CompletedTask;
            }

            var validated = ValidateClaims(context.User);
            if (validated && context.User.IsInRole(UserRoles.Administrator))
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
