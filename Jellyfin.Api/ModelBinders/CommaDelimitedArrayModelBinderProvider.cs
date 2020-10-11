using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Jellyfin.Api.ModelBinders
{
    /// <summary>
    /// Comma delimited array model binder provider.
    /// </summary>
    public class CommaDelimitedArrayModelBinderProvider : IModelBinderProvider
    {
        private readonly IModelBinder _binder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommaDelimitedArrayModelBinderProvider"/> class.
        /// </summary>
        public CommaDelimitedArrayModelBinderProvider()
        {
            _binder = new CommaDelimitedArrayModelBinder();
        }

        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            return context.Metadata.ModelType.IsArray ? _binder : null;
        }
    }
}
