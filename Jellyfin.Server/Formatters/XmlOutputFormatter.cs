using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Jellyfin.Server.Formatters
{
    /// <summary>
    /// Css output formatter.
    /// </summary>
    public class XmlOutputFormatter : TextOutputFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlOutputFormatter"/> class.
        /// </summary>
        public XmlOutputFormatter()
        {
            SupportedMediaTypes.Add("text/xml;charset=UTF-8");
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        /// <summary>
        /// Write context object to stream.
        /// </summary>
        /// <param name="context">Writer context.</param>
        /// <param name="selectedEncoding">Unused. Writer encoding.</param>
        /// <returns>Write stream task.</returns>
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            return context.HttpContext.Response.WriteAsync(context.Object?.ToString());
        }
    }
}
