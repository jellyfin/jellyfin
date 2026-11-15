using System;
using System.Linq;
using System.Reflection;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Controllers;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

/// <summary>
/// Guards the download route: it must be reachable by a share token (so "Copy Stream URL"
/// can hand out a scoped link instead of the caller's session credential), and any marked
/// route must bind an item so the share-link policy can scope it.
/// </summary>
public sealed class ShareLinkMarkedRoutesTests
{
    [Fact]
    public void GetDownload_IsShareLinkAccessibleAndItemBound()
    {
        var method = typeof(LibraryController).GetMethod(nameof(LibraryController.GetDownload));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<ShareLinkAccessAttribute>());

        var policies = GetPolicies(method);
        Assert.Contains(Policies.ShareLink, policies);
        Assert.Contains(Policies.Download, policies);

        var route = method.GetCustomAttribute<HttpGetAttribute>()?.Template;
        Assert.NotNull(route);
        Assert.Contains("{itemId}", route, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MarkedActions_AlwaysBindAnItemRouteValue()
    {
        var markedActions = typeof(ShareLinkAccessAttribute).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.GetCustomAttribute<ShareLinkAccessAttribute>() is not null);

        foreach (var action in markedActions)
        {
            var template = action.GetCustomAttribute<HttpGetAttribute>()?.Template
                ?? string.Empty;

            // The share-link policy resolves the item from "itemId" or "routeItemId" and
            // fails closed otherwise, so a marked route without one is always denied.
            Assert.True(
                template.Contains("{itemId}", StringComparison.OrdinalIgnoreCase)
                || template.Contains("{routeItemId}", StringComparison.OrdinalIgnoreCase),
                $"[ShareLinkAccess] action {action.DeclaringType?.Name}.{action.Name} binds no item route value.");
        }
    }

    private static string[] GetPolicies(MethodInfo method)
    {
        return method.GetCustomAttributes<AuthorizeAttribute>()
            .Select(a => a.Policy ?? string.Empty)
            .Where(p => p.Length > 0)
            .ToArray();
    }
}
