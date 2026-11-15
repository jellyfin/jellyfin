using System;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Jellyfin.Api.Attributes;

/// <summary>
/// Marks an endpoint as accessible with a scoped share link token.
/// </summary>
/// <remarks>
/// The attribute is applied to controller actions, so MVC exposes it through the
/// <see cref="ControllerActionDescriptor"/> metadata rather than as a standalone metadata item.
/// Authentication inspects both, otherwise share links fail with a bare 401.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ShareLinkAccessAttribute : Attribute
{
    /// <summary>
    /// Whether the given endpoint is marked for share-link access.
    /// </summary>
    /// <param name="endpoint">The endpoint being authenticated, if routing has run.</param>
    /// <returns><c>true</c> when the endpoint carries this attribute.</returns>
    public static bool IsMarked(Microsoft.AspNetCore.Http.Endpoint? endpoint)
    {
        if (endpoint is null)
        {
            return false;
        }

        if (endpoint.Metadata.GetMetadata<ShareLinkAccessAttribute>() is not null)
        {
            return true;
        }

        var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action is null)
        {
            return false;
        }

        return action.MethodInfo.GetCustomAttributes(typeof(ShareLinkAccessAttribute), true).Length > 0
            || action.ControllerTypeInfo.GetCustomAttributes(typeof(ShareLinkAccessAttribute), true).Length > 0;
    }
}
