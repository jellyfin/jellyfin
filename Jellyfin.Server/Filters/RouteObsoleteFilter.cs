using System;
using System.Linq;
using Jellyfin.Api.Attributes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters
{
    /// <summary>
    /// Mark a route as deprecated if its action carries a <see cref="RouteObsoleteAttribute"/> naming it.
    /// </summary>
    public class RouteObsoleteFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var routeName = context.ApiDescription.ActionDescriptor.AttributeRouteInfo?.Name;
            if (routeName is null)
            {
                return;
            }

            var obsoleteRoutes = context.MethodInfo?
                .GetCustomAttributes(typeof(RouteObsoleteAttribute), true)
                .OfType<RouteObsoleteAttribute>()
                .FirstOrDefault();

            if (obsoleteRoutes is not null && obsoleteRoutes.RouteNames.Contains(routeName, StringComparer.Ordinal))
            {
                operation.Deprecated = true;
            }
        }
    }
}
