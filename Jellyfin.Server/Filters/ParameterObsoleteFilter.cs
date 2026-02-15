using System;
using System.Linq;
using Jellyfin.Api.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters
{
    /// <summary>
    /// Mark parameter as deprecated if it has the <see cref="ParameterObsoleteAttribute"/>.
    /// </summary>
    public class ParameterObsoleteFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var parameterDescription in context.ApiDescription.ParameterDescriptions)
            {
                if (parameterDescription
                    .CustomAttributes()
                    .OfType<ParameterObsoleteAttribute>()
                    .Any())
                {
                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter.Name.Equals(parameterDescription.Name, StringComparison.Ordinal))
                        {
                            parameter.Deprecated = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
