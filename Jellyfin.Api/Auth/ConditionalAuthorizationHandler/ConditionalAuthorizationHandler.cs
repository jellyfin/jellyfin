using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Auth.ConditionalAuthorizationHandler;

/// <summary>
/// Authorization handler for conditionally requiring elevated access to specific routes.
/// </summary>
public class ConditionalAuthorizationHandler : AuthorizationHandler<ConditionalAuthorizationRequirement>
{
    private readonly IServerConfigurationManager _configurationManager;
    private readonly ILogger<ConditionalAuthorizationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    public ConditionalAuthorizationHandler(IServerConfigurationManager configurationManager, ILogger<ConditionalAuthorizationHandler> logger)
    {
        _configurationManager = configurationManager;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ConditionalAuthorizationRequirement requirement)
    {
        var experimentalConfiguration = (ExperimentalConfiguration)_configurationManager.GetConfiguration("experimental");
        var requiresElevation = requirement.PolicyName == Policies.ElevatePlugin || requirement.PolicyName == Policies.ElevateConfiguration;
        var conditionalAuthorization = requirement.PolicyName switch
        {
            Policies.ElevatePlugin => experimentalConfiguration.ElevatePluginRoutes,
            Policies.ElevateConfiguration => experimentalConfiguration.ElevateConfigurationRoutes,
            Policies.AuthenticateAudio => experimentalConfiguration.AuthenticateAudioRoutes,
            Policies.AuthenticateVideo => experimentalConfiguration.AuthenticateVideoRoutes,
            _ => true
        };

        if (conditionalAuthorization && requiresElevation && !context.User.IsInRole(UserRoles.Administrator))
        {
            _logger.LogWarning("administrator access currently required for this route due to {PolicyName} setting", requirement.PolicyName);
            context.Fail();
        }
        else if (conditionalAuthorization && !requiresElevation && !context.User.Identity!.IsAuthenticated)
        {
            _logger.LogWarning("authentication currently required for this route due to {PolicyName} setting", requirement.PolicyName);
            context.Fail();
        }
        else
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
