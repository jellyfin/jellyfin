using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.ModelBinders;

/// <summary>
/// Binds all values for a key to a string using the standard <see cref="Microsoft.Extensions.Primitives.StringValues"/> conversion.
/// </summary>
public sealed class CommaJoinedStringModelBinder : IModelBinder
{
    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        bindingContext.Result = ModelBindingResult.Success((string?)value.Values);
        return Task.CompletedTask;
    }
}
