using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Jellyfin.Server.Filters;

/// <summary>
/// Document filter that fixes security scheme references after document generation.
/// </summary>
/// <remarks>
/// In Microsoft.OpenApi v2, <see cref="OpenApiSecuritySchemeReference"/> requires a resolved
/// <c>Target</c> to serialize correctly. References created without a host document (as in
/// operation filters) serialize as empty objects. This filter re-creates all security scheme
/// references with the document context so they resolve properly during serialization.
/// </remarks>
internal class SecuritySchemeReferenceFixupFilter : IDocumentFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.RegisterComponents();

        if (swaggerDoc.Paths is null)
        {
            return;
        }

        foreach (var pathItem in swaggerDoc.Paths.Values)
        {
            if (pathItem.Operations is null)
            {
                continue;
            }

            foreach (var operation in pathItem.Operations.Values)
            {
                if (operation.Security is null)
                {
                    continue;
                }

                for (int i = 0; i < operation.Security.Count; i++)
                {
                    var oldReq = operation.Security[i];
                    var newReq = new OpenApiSecurityRequirement();
                    foreach (var kvp in oldReq)
                    {
                        var fixedRef = new OpenApiSecuritySchemeReference(kvp.Key.Reference.Id!, swaggerDoc);
                        newReq[fixedRef] = kvp.Value;
                    }

                    operation.Security[i] = newReq;
                }
            }
        }
    }
}
