using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Jellyfin.Api.Formatters;

/// <summary>
/// Css output formatter.
/// </summary>
public class CssOutputFormatter : TextOutputFormatter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssOutputFormatter"/> class.
    /// </summary>
    public CssOutputFormatter()
    {
        SupportedMediaTypes.Add("text/css");

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
        var stringResponse = context.Object?.ToString();
        return stringResponse is null ? Task.CompletedTask : context.HttpContext.Response.WriteAsync(stringResponse);
    }
}
