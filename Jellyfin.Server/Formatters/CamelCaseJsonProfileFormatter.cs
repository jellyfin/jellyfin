using System.Net.Mime;
using System.Text;
using MediaBrowser.Common.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Server.Formatters
{
    /// <summary>
    /// Camel Case Json Profile Formatter.
    /// </summary>
    public class CamelCaseJsonProfileFormatter : SystemTextJsonOutputFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CamelCaseJsonProfileFormatter"/> class.
        /// </summary>
        public CamelCaseJsonProfileFormatter() : base(JsonDefaults.GetCamelCaseOptions())
        {
            SupportedMediaTypes.Clear();
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json; profile=\"CamelCase\""));
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json; charset=utf-8"));
        }
    }
}
