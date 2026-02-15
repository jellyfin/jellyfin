using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.ModelBinders;

/// <summary>
/// Nullable enum model binder.
/// </summary>
public class NullableEnumModelBinder : IModelBinder
{
    private readonly ILogger<NullableEnumModelBinder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NullableEnumModelBinder"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{NullableEnumModelBinder}"/> interface.</param>
    public NullableEnumModelBinder(ILogger<NullableEnumModelBinder> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        var elementType = bindingContext.ModelType.GetElementType() ?? bindingContext.ModelType.GenericTypeArguments[0];
        var converter = TypeDescriptor.GetConverter(elementType);
        if (valueProviderResult.Length != 0)
        {
            try
            {
                // REVIEW: This shouldn't be null here
                var convertedValue = converter.ConvertFromString(valueProviderResult.FirstValue!);
                bindingContext.Result = ModelBindingResult.Success(convertedValue);
            }
            catch (FormatException e)
            {
                _logger.LogDebug(e, "Error converting value.");
            }
        }

        return Task.CompletedTask;
    }
}
