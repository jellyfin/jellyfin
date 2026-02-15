using System.Collections.Generic;
using Jellyfin.Api.Attributes;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters
{
    /// <inheritdoc />
    public class FileRequestFilter : IOperationFilter
    {
        /// <inheritdoc />
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var attribute in context.ApiDescription.ActionDescriptor.EndpointMetadata)
            {
                if (attribute is AcceptsFileAttribute acceptsFileAttribute)
                {
                    operation.RequestBody = GetRequestBody(acceptsFileAttribute.ContentTypes);
                    break;
                }
            }
        }

        private static OpenApiRequestBody GetRequestBody(IEnumerable<string> contentTypes)
        {
            var body = new OpenApiRequestBody();
            var mediaType = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "binary"
                }
            };
            foreach (var contentType in contentTypes)
            {
                body.Content.Add(contentType, mediaType);
            }

            return body;
        }
    }
}
