using System;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Jellyfin.Api.Formatters;

/// <summary>
/// Xml output formatter.
/// </summary>
public sealed class XmlOutputFormatter : TextOutputFormatter
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
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEncoding);

        var valueAsString = context.Object?.ToString();
        if (string.IsNullOrEmpty(valueAsString))
        {
            return;
        }

        var response = context.HttpContext.Response;
        await response.WriteAsync(valueAsString, selectedEncoding).ConfigureAwait(false);
    }
}
