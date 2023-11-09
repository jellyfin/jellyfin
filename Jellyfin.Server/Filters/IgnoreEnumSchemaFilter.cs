using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfin.Data.Attributes;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters;

/// <summary>
/// Filter to remove ignored enum values.
/// </summary>
public class IgnoreEnumSchemaFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum || (Nullable.GetUnderlyingType(context.Type)?.IsEnum ?? false))
        {
            var type = context.Type.IsEnum ? context.Type : Nullable.GetUnderlyingType(context.Type);
            if (type is null)
            {
                return;
            }

            var enumOpenApiStrings = new List<IOpenApiAny>();

            foreach (var enumName in Enum.GetNames(type))
            {
                var member = type.GetMember(enumName)[0];
                if (!member.GetCustomAttributes<OpenApiIgnoreEnumAttribute>().Any())
                {
                    enumOpenApiStrings.Add(new OpenApiString(enumName));
                }
            }

            schema.Enum = enumOpenApiStrings;
        }
    }
}
