using System;
using System.Linq;
using Jellyfin.Api.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters
{
    /// <inheritdoc />
    public class FileResponseFilter : IOperationFilter
    {
        private const string SuccessCode = "200";
        private static readonly OpenApiMediaType _openApiMediaType = new OpenApiMediaType
        {
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            }
        };

        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var attribute in context.ApiDescription.ActionDescriptor.EndpointMetadata)
            {
                if (attribute is ProducesFileAttribute producesFileAttribute)
                {
                    // Get operation response values.
                    var response = operation.Responses
                        .FirstOrDefault(o => o.Key.Equals(SuccessCode, StringComparison.Ordinal));

                    // Operation doesn't have a response.
                    if (response.Value is null)
                    {
                        continue;
                    }

                    // Clear existing responses.
                    response.Value.Content.Clear();

                    // Add all content-types as file.
                    foreach (var contentType in producesFileAttribute.ContentTypes)
                    {
                        response.Value.Content.Add(contentType, _openApiMediaType);
                    }

                    break;
                }
            }
        }
    }
}
