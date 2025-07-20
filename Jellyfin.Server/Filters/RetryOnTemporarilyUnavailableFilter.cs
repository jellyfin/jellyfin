using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters;

internal class RetryOnTemporarilyUnavailableFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.Add("503", new OpenApiResponse()
        {
            Description = "The server is currently starting or is temporarily not available.",
            Headers = new Dictionary<string, OpenApiHeader>()
            {
                {
                    "Retry-After",
                    new() { AllowEmptyValue = true, Required = false, Description = "A hint for when to retry the operation in full seconds." }
                },
                {
                    "Message",
                    new() { AllowEmptyValue = true, Required = false, Description = "A short plain-text reason why the server is not available." }
                }
            },
            Content = new Dictionary<string, OpenApiMediaType>()
            {
                {
                    "text/html",
                    new OpenApiMediaType()
                }
            }
        });
    }
}
