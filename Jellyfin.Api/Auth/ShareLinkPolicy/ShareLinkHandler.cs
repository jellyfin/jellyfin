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
    /// <summary>
    /// Route value names that bind a media item on endpoints a share token may reach.
    /// </summary>
    private static readonly string[] RouteItemIdKeys = ["itemId", "routeItemId"];

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
        if (routeItemId is null)
        {
            // Fail closed: a marked endpoint that binds no media item cannot be scoped to the
            // token, so a share credential must never satisfy it.
            context.Fail();
            return Task.CompletedTask;
        }

        if (routeItemId.Value.Equals(context.User.GetShareItemId()))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves the media item the current route operates on.
    /// </summary>
    /// <remarks>
    /// Every route parameter name that binds an item is checked, so a route that uses a name
    /// other than "itemId" is still scoped to the token.
    /// </remarks>
    /// <returns>The bound item id, or <c>null</c> when the route binds no item.</returns>
    private Guid? GetRouteItemId()
    {
        var routeValues = _httpContextAccessor.HttpContext?.Request.RouteValues;
        if (routeValues is null)
        {
            return null;
        }

        foreach (var name in RouteItemIdKeys)
        {
            if (routeValues.TryGetValue(name, out var value)
                && Guid.TryParse(value?.ToString(), out var itemId))
            {
                return itemId;
            }
        }

        return null;
    }
}
