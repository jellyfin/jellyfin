using System;
using System.Linq;
using Jellyfin.Api.Attributes;
using Microsoft.OpenApi;
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
                    if (operation.Parameters is null)
                    {
                        continue;
                    }

                    foreach (var parameter in operation.Parameters)
                    {
                        if (parameter is OpenApiParameter concreteParam
                            && string.Equals(concreteParam.Name, parameterDescription.Name, StringComparison.Ordinal))
                        {
                            concreteParam.Deprecated = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
