using System.Xml.Serialization;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Model.Dlna;

/// <summary>
/// Defines the <see cref="DirectPlayProfile"/>.
/// </summary>
public class DirectPlayProfile
{
    /// <summary>
    /// Gets or sets the container.
    /// </summary>
    [XmlAttribute("container")]
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the audio codec.
    /// </summary>
    [XmlAttribute("audioCodec")]
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the video codec.
    /// </summary>
    [XmlAttribute("videoCodec")]
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the Dlna profile type.
    /// </summary>
    [XmlAttribute("type")]
    public DlnaProfileType Type { get; set; }

    /// <summary>
    /// Returns whether the <see cref="Container"/> supports the <paramref name="container"/>.
    /// </summary>
    /// <param name="container">The container to match against.</param>
    /// <returns>True if supported.</returns>
    public bool SupportsContainer(string? container)
    {
        return ContainerHelper.ContainsContainer(Container, container);
    }

    /// <summary>
    /// Returns whether the <see cref="VideoCodec"/> supports the <paramref name="codec"/>.
    /// </summary>
    /// <param name="codec">The codec to match against.</param>
    /// <returns>True if supported.</returns>
    public bool SupportsVideoCodec(string? codec)
    {
        return Type == DlnaProfileType.Video && ContainerHelper.ContainsContainer(VideoCodec, codec);
    }

    /// <summary>
    /// Returns whether the <see cref="AudioCodec"/> supports the <paramref name="codec"/>.
    /// </summary>
    /// <param name="codec">The codec to match against.</param>
    /// <returns>True if supported.</returns>
    public bool SupportsAudioCodec(string? codec)
    {
        // Video profiles can have audio codec restrictions too, therefore include Video as valid type.
        return (Type == DlnaProfileType.Audio || Type == DlnaProfileType.Video) && ContainerHelper.ContainsContainer(AudioCodec, codec);
    }
}
