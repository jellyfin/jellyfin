using System;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Api.Auth.ShareLinkPolicy;

/// <summary>
/// Authorization handler for <see cref="ShareLinkRequirement"/>.
/// </summary>
public class ShareLinkHandler : AuthorizationHandler<ShareLinkRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareLinkHandler"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
    public ShareLinkHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ShareLinkRequirement requirement)
    {
        if (!context.User.GetIsShareLink())
        {
            // Requests without a share link claim keep using the normal authentication result.
            if (context.User.Identity?.IsAuthenticated ?? false)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }

        var routeItemId = GetRouteItemId();
        if (routeItemId is null || routeItemId.Value.Equals(context.User.GetShareItemId()))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }

    private Guid? GetRouteItemId()
    {
        var routeValues = _httpContextAccessor.HttpContext?.Request.RouteValues;
        if (routeValues is not null
            && routeValues.TryGetValue("itemId", out var value)
            && Guid.TryParse(value?.ToString(), out var itemId))
        {
            return itemId;
        }

        return null;
    }
}
