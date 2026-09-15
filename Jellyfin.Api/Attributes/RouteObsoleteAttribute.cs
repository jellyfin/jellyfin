using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Attributes;

/// <summary>
/// Attribute to mark named routes of an action as obsolete.
/// </summary>
/// <remarks>
/// For an action answering on both a current route and the route it replaced, where
/// <see cref="ObsoleteAttribute"/> would deprecate every route the action has.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RouteObsoleteAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RouteObsoleteAttribute"/> class.
    /// </summary>
    /// <param name="routeNames">The names of the obsolete routes.</param>
    public RouteObsoleteAttribute(params string[] routeNames)
    {
        RouteNames = routeNames;
    }

    /// <summary>
    /// Gets the names of the obsolete routes.
    /// </summary>
    public IReadOnlyList<string> RouteNames { get; }
}
