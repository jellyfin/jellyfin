using System.Collections.Generic;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters;

internal class RetryOnTemporarilyUnavailableFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses?.TryAdd(
            "503",
            new OpenApiResponse
            {
                Description = "The server is currently starting or is temporarily not available.",
                Headers = new Dictionary<string, IOpenApiHeader>
                {
                    {
                        "Retry-After", new OpenApiHeader
                        {
                            AllowEmptyValue = true,
                            Required = false,
                            Description = "A hint for when to retry the operation in full seconds.",
                            Schema = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Integer,
                                Format = "int32"
                            }
                        }
                    },
                    {
                        "Message", new OpenApiHeader
                        {
                            AllowEmptyValue = true,
                            Required = false,
                            Description = "A short plain-text reason why the server is not available.",
                            Schema = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "text"
                            }
                        }
                    }
                },
                Content = new Dictionary<string, OpenApiMediaType>()
                {
                    { "text/html", new OpenApiMediaType() }
                }
            });
    }
}
