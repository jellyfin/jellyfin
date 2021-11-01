using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Jellyfin.Api.Attributes
{
    /// <summary>
    /// Identifies an action that supports the HTTP GET method.
    /// </summary>
    public class HttpUnsubscribeAttribute : HttpMethodAttribute
    {
        private static readonly IEnumerable<string> _supportedMethods = new[] { "UNSUBSCRIBE" };

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpUnsubscribeAttribute"/> class.
        /// </summary>
        public HttpUnsubscribeAttribute()
            : base(_supportedMethods)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpUnsubscribeAttribute"/> class.
        /// </summary>
        /// <param name="template">The route template. May not be null.</param>
        public HttpUnsubscribeAttribute(string template)
            : base(_supportedMethods, template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }
        }
    }
}
