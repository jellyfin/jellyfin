using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.ModelBinders;

/// <summary>
/// DateTime model binder.
/// </summary>
public class LegacyDateTimeModelBinder : IModelBinder
{
    // Borrowed from the DateTimeModelBinderProvider
    private const DateTimeStyles SupportedStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;
    private readonly DateTimeModelBinder _defaultModelBinder;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyDateTimeModelBinder"/> class.
    /// </summary>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    public LegacyDateTimeModelBinder(ILoggerFactory loggerFactory)
    {
        _defaultModelBinder = new DateTimeModelBinder(SupportedStyles, loggerFactory);
    }

    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult.Values.Count == 1)
        {
            var dateTimeString = valueProviderResult.FirstValue;
            // Mark Played Item.
            if (DateTime.TryParseExact(dateTimeString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
            {
                bindingContext.Result = ModelBindingResult.Success(dateTime);
            }
            else
            {
                return _defaultModelBinder.BindModelAsync(bindingContext);
            }
        }

        return Task.CompletedTask;
    }
}
