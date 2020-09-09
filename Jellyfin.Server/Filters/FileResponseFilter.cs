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
                Type = "file"
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
                    var (_, value) = operation.Responses
                        .FirstOrDefault(o => o.Key.Equals(SuccessCode, StringComparison.Ordinal));

                    // Operation doesn't have a response.
                    if (value == null)
                    {
                        continue;
                    }

                    // Clear existing responses.
                    value.Content.Clear();

                    // Add all content-types as file.
                    foreach (var contentType in producesFileAttribute.GetContentTypes())
                    {
                        value.Content.Add(contentType, _openApiMediaType);
                    }

                    break;
                }
            }
        }
    }
}
