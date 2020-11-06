using System;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters
{
    /// <inheritdoc />
    public class ObsoleteRoutesFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor actionDescriptor
                && !string.IsNullOrEmpty(actionDescriptor.AttributeRouteInfo.Name)
                && actionDescriptor.AttributeRouteInfo.Name.EndsWith("deprecated", StringComparison.OrdinalIgnoreCase))
            {
                operation.Deprecated = true;
            }
        }
    }
}
