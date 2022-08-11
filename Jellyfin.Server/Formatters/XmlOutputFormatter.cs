using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Jellyfin.Server.Formatters
{
    /// <summary>
    /// Xml output formatter.
    /// </summary>
    public class XmlOutputFormatter : TextOutputFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlOutputFormatter"/> class.
        /// </summary>
        public XmlOutputFormatter()
        {
            SupportedMediaTypes.Clear();
            SupportedMediaTypes.Add(MediaTypeNames.Text.Xml);

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        /// <inheritdoc />
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var stringResponse = context.Object?.ToString();
            return stringResponse == null ? Task.CompletedTask : context.HttpContext.Response.WriteAsync(stringResponse);
        }
    }
}
