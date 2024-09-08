using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.ModelBinders;

/// <summary>
/// Nullable enum model binder provider.
/// </summary>
public class NullableEnumModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var nullableType = Nullable.GetUnderlyingType(context.Metadata.ModelType);
        if (nullableType is null || !nullableType.IsEnum)
        {
            // Type isn't nullable or isn't an enum.
            return null;
        }

        var logger = context.Services.GetRequiredService<ILogger<NullableEnumModelBinder>>();
        return new NullableEnumModelBinder(logger);
    }
}
