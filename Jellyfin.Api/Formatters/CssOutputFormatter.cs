using System.Net.Mime;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Jellyfin.Api.Formatters;

/// <summary>
/// Css output formatter.
/// </summary>
public sealed class CssOutputFormatter : StringOutputFormatter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CssOutputFormatter"/> class.
    /// </summary>
    public CssOutputFormatter()
    {
        SupportedMediaTypes.Clear();
        SupportedMediaTypes.Add(MediaTypeNames.Text.Css);
    }
}
