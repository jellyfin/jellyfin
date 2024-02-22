using System;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters;

/// <summary>
/// Fixes FromForm parameter schema references.
/// </summary>
public class FromFormOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        // IFormFile is only used in POST and PUT.
        if (!string.Equals(context.ApiDescription.HttpMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(context.ApiDescription.HttpMethod, HttpMethods.Put, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Ensure the endpoint consumes multipart/form-data.
        var consumesAttribute = context.MethodInfo.GetCustomAttribute<ConsumesAttribute>(inherit: true);
        if (consumesAttribute is null
            || !consumesAttribute.ContentTypes.Contains(MediaTypeNames.Multipart.FormData, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        // Locate the form parameter.
        var formParameter = context.MethodInfo.GetParameters().FirstOrDefault(p => p.GetCustomAttribute<FromFormAttribute>() is not null);
        if (formParameter is null)
        {
            return;
        }

        // Get or generate the schema for the model.
        if (!context.SchemaRepository.Schemas.TryGetValue(formParameter.ParameterType.Name, out var schema))
        {
            schema = context.SchemaGenerator.GenerateSchema(formParameter.ParameterType, context.SchemaRepository);
        }

        // Attach the operation to the new schema reference.
        var mediaType = operation.RequestBody.Content.Values.First();
        mediaType.Schema = new OpenApiSchema { Reference = schema.Reference };
        mediaType.Encoding.Clear();
    }
}
