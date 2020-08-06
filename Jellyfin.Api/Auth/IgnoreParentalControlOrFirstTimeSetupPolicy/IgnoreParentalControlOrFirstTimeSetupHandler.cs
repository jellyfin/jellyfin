using System.Threading.Tasks;
using Jellyfin.Api.Auth.IgnoreParentalControlPolicy;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.IgnoreParentalControlOrFirstTimeSetupPolicy
{
    /// <summary>
    /// Escape schedule controls handler.
    /// </summary>
    public class IgnoreParentalControlOrFirstTimeSetupHandler : BaseAuthorizationHandler<IgnoreParentalControlRequirement>
    {
        private readonly IConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="IgnoreParentalControlOrFirstTimeSetupHandler"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
        /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        public IgnoreParentalControlOrFirstTimeSetupHandler(
            IUserManager userManager,
            INetworkManager networkManager,
            IHttpContextAccessor httpContextAccessor,
            IConfigurationManager configurationManager)
            : base(userManager, networkManager, httpContextAccessor)
        {
            _configurationManager = configurationManager;
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IgnoreParentalControlRequirement requirement)
        {
            var validated = ValidateClaims(context.User, ignoreSchedule: true);
            if (validated || !_configurationManager.CommonConfiguration.IsStartupWizardCompleted)
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
