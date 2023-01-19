using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.LiveTvManagementPolicy;

/// <summary>
/// Authorization handler for LiveTV management access.
/// </summary>
public class LiveTvManagementHandler : BaseAuthorizationHandler<LiveTvManagementRequirement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiveTvManagementHandler"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
    /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
    public LiveTvManagementHandler(
        IUserManager userManager,
        INetworkManager networkManager,
        IHttpContextAccessor httpContextAccessor)
        : base(userManager, networkManager, httpContextAccessor)
    {
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LiveTvManagementRequirement requirement)
    {
        var validated = ValidateClaims(context.User, requireLiveTvManagementPermission: true);
        if (validated)
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
