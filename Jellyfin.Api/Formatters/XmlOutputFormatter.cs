using System.Net.Mime;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Jellyfin.Api.Formatters;

/// <summary>
/// Xml output formatter.
/// </summary>
public sealed class XmlOutputFormatter : StringOutputFormatter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlOutputFormatter"/> class.
    /// </summary>
    public XmlOutputFormatter()
    {
        SupportedMediaTypes.Clear();
        SupportedMediaTypes.Add(MediaTypeNames.Text.Xml);
    }
}
