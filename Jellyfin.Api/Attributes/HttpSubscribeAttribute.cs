using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Jellyfin.Api.Attributes
{
    /// <summary>
    /// Identifies an action that supports the HTTP GET method.
    /// </summary>
    public sealed class HttpSubscribeAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "SUBSCRIBE" };

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpSubscribeAttribute"/> class.
        /// </summary>
        public HttpSubscribeAttribute()
            : base(_supportedMethods)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpSubscribeAttribute"/> class.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpSubscribeAttribute(string template)
            : base(_supportedMethods, template)
            => ArgumentNullException.ThrowIfNull(template, nameof(template));
    }
}
