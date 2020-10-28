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
            var elementType = bindingContext.ModelType.GetElementType();
            var converter = TypeDescriptor.GetConverter(elementType);

            if (valueProviderResult.Length > 1)
            {
                var result = Array.CreateInstance(elementType, valueProviderResult.Length);

                for (int i = 0; i < valueProviderResult.Length; i++)
                {
                    var value = converter.ConvertFromString(valueProviderResult.Values[i].Trim());

                    result.SetValue(value, i);
                }

                bindingContext.Result = ModelBindingResult.Success(result);
            }
            else
            {
                var value = valueProviderResult.FirstValue;

                if (value != null)
                {
                    var values = Array.ConvertAll(
                        value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
                        x => converter.ConvertFromString(x?.Trim()));

                    var typedValues = Array.CreateInstance(elementType, values.Length);
                    values.CopyTo(typedValues, 0);

                    bindingContext.Result = ModelBindingResult.Success(typedValues);
                }
                else
                {
                    var emptyResult = Array.CreateInstance(elementType, 0);
                    bindingContext.Result = ModelBindingResult.Success(emptyResult);
                }
            }

            return Task.CompletedTask;
        }
    }
}
