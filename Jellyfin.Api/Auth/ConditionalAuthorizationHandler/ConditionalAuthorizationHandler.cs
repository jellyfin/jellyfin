using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace Jellyfin.Api.Auth.ConditionalAuthorizationHandler;

/// <summary>
/// Authorization handler for conditionally requiring elevated access to specific routes.
/// </summary>
public class ConditionalAuthorizationHandler : AuthorizationHandler<ConditionalAuthorizationRequirement>
{
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConditionalAuthorizationHandler"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public ConditionalAuthorizationHandler(IServerConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
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
            context.Fail();
        }
        else if (conditionalAuthorization && !requiresElevation && !context.User.Identity!.IsAuthenticated)
        {
            context.Fail();
        }
        else
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
