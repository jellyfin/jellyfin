using System;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters;

/// <summary>
/// Schema filter to ensure flags enums are represented correctly in OpenAPI.
/// </summary>
/// <remarks>
/// For flags enums:
/// - The enum schema definition is set to type "string" (not integer).
/// - Properties using flags enums are transformed to arrays referencing the enum schema.
/// </remarks>
public class FlagsEnumSchemaFilter : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type.IsEnum ? context.Type : Nullable.GetUnderlyingType(context.Type);
        if (type is null || !type.IsEnum)
        {
            return;
        }

        // Check if enum has [Flags] attribute
        if (!type.IsDefined(typeof(FlagsAttribute), false))
        {
            return;
        }

        if (schema is not OpenApiSchema concreteSchema)
        {
            return;
        }

        if (context.MemberInfo is null)
        {
            // Processing the enum definition itself - ensure it's type "string" not "integer"
            concreteSchema.Type = JsonSchemaType.String;
            concreteSchema.Format = null;
        }
        else
        {
            // Processing a property that uses the flags enum - transform to array
            // Generate the enum schema to ensure it exists in the repository
            var enumSchema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

            // Flags enums should be represented as arrays referencing the enum schema
            // since multiple values can be combined
            concreteSchema.Type = JsonSchemaType.Array;
            concreteSchema.Format = null;
            concreteSchema.Enum = null;
            concreteSchema.AllOf = null;
            concreteSchema.Items = enumSchema;
        }
    }
}
