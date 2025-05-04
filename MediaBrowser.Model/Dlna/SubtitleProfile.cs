#nullable disable

using System.Xml.Serialization;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Model.Dlna;

/// <summary>
/// A class for subtitle profile information.
/// </summary>
public class SubtitleProfile
{
    /// <summary>
    /// Gets or sets the format.
    /// </summary>
    [XmlAttribute("format")]
    public string Format { get; set; }

    /// <summary>
    /// Gets or sets the delivery method.
    /// </summary>
    [XmlAttribute("method")]
    public SubtitleDeliveryMethod Method { get; set; }

    /// <summary>
    /// Gets or sets the DIDL mode.
    /// </summary>
    [XmlAttribute("didlMode")]
    public string DidlMode { get; set; }

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    [XmlAttribute("language")]
    public string Language { get; set; }

    /// <summary>
    /// Gets or sets the container.
    /// </summary>
    [XmlAttribute("container")]
    public string Container { get; set; }

    /// <summary>
    /// Checks if a language is supported.
    /// </summary>
    /// <param name="subLanguage">The language to check for support.</param>
    /// <returns><c>true</c> if supported.</returns>
    public bool SupportsLanguage(string subLanguage)
    {
        if (string.IsNullOrEmpty(Language))
        {
            return true;
        }

        if (string.IsNullOrEmpty(subLanguage))
        {
            subLanguage = "und";
        }

        return ContainerHelper.ContainsContainer(Language, subLanguage);
    }
}
