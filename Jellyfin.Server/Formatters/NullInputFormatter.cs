using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Jellyfin.Server.Formatters
{
    /// <summary>
    /// Permits input from content-type null Gets.
    /// </summary>
    public class NullInputFormatter : TextInputFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullInputFormatter"/> class.
        /// </summary>
        public NullInputFormatter()
        {
            SupportedMediaTypes.Add("text/css");

            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        /// <summary>
        /// Allow text/xml and text/xml;charset=UTF-8 to be processed.
        /// </summary>
        /// <param name="context">The <see cref="InputFormatterContext"/> instance.</param>
        /// <returns>True if we can read this data.</returns>
        public override bool CanRead(InputFormatterContext context)
        {
            return (context.HttpContext.Request.ContentType == null) && (context.HttpContext.Request.ContentLength == 0);
        }

        /// <summary>
        /// Dummy routine.
        /// </summary>
        /// <param name="context">The <see cref="InputFormatterContext"/> instance.</param>
        /// <param name="encoding">The <see cref="Encoding"/> type.</param>
        /// <returns>The <see cref="InputFormatterResult"/> result.</returns>
        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            // If it gets this far, something is definitely wrong and we don't want to continue.
            throw new NotImplementedException();
        }
    }
}
