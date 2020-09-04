#pragma warning disable SA1611
#pragma warning disable SA1615

using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Jellyfin.Server.Formatters
{
    /// <summary>
    /// Xml input formatter.
    /// </summary>
    public class XmlInputFormatter : TextInputFormatter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlInputFormatter"/> class.
        /// </summary>
        public XmlInputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeNames.Text.Xml);
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        /// <summary>
        /// Allow text/xml and text/xml;charset=UTF-8 to be processed.
        /// </summary>
        public override bool CanRead(InputFormatterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var contentType = context.HttpContext.Request.ContentType;
            return contentType.StartsWith(MediaTypeNames.Text.Xml, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Handle text/xml content types.
        /// </summary>
        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var request = context.HttpContext.Request;
            var contentType = context.HttpContext.Request.ContentType;

            if (contentType.StartsWith(MediaTypeNames.Text.Xml, StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new StreamReader(request.Body))
                {
                    var content = await reader.ReadToEndAsync().ConfigureAwait(false);
                    return await InputFormatterResult.SuccessAsync(content).ConfigureAwait(false);
                }
            }

            return await InputFormatterResult.FailureAsync().ConfigureAwait(false);
        }
    }
}
