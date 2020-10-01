using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.ModelBinders
{
    /// <summary>
    /// Comma delimited array model binder.
    /// Returns an empty array of specified type if there is no query parameter.
    /// </summary>
    public class CommaDelimitedArrayModelBinder : IModelBinder
    {
        /// <inheritdoc/>
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            var input = valueProviderResult.FirstValue;
            var elementType = bindingContext.ModelType.GetElementType();

            if (input != null)
            {
                var converter = TypeDescriptor.GetConverter(elementType);
                var values = Array.ConvertAll(
                    input.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
                    x => { return converter.ConvertFromString(x != null ? x.Trim() : x); });

                var typedValues = Array.CreateInstance(elementType, values.Length);
                values.CopyTo(typedValues, 0);

                bindingContext.Result = ModelBindingResult.Success(typedValues);
            }
            else
            {
                var emptyResult = Array.CreateInstance(elementType, 0);
                bindingContext.Result = ModelBindingResult.Success(emptyResult);
            }

            return Task.CompletedTask;
        }
    }
}
