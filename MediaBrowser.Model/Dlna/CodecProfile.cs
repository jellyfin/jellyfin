using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Model.Dlna;

/// <summary>
/// Defines the <see cref="CodecProfile"/>.
/// </summary>
public class CodecProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodecProfile"/> class.
    /// </summary>
    public CodecProfile()
    {
        Conditions = [];
        ApplyConditions = [];
    }

    /// <summary>
    /// Gets or sets the <see cref="CodecType"/> which this container must meet.
    /// </summary>
    [XmlAttribute("type")]
    public CodecType Type { get; set; }

    /// <summary>
    /// Gets or sets the list of <see cref="ProfileCondition"/> which this profile must meet.
    /// </summary>
    public ProfileCondition[] Conditions { get; set; }

    /// <summary>
    /// Gets or sets the list of <see cref="ProfileCondition"/> to apply if this profile is met.
    /// </summary>
    public ProfileCondition[] ApplyConditions { get; set; }

    /// <summary>
    /// Gets or sets the codec(s) that this profile applies to.
    /// </summary>
    [XmlAttribute("codec")]
    public string? Codec { get; set; }

    /// <summary>
    /// Gets or sets the container(s) which this profile will be applied to.
    /// </summary>
    [XmlAttribute("container")]
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the sub-container(s) which this profile will be applied to.
    /// </summary>
    [XmlAttribute("subcontainer")]
    public string? SubContainer { get; set; }

    /// <summary>
    /// Checks to see whether the codecs and containers contain the given parameters.
    /// </summary>
    /// <param name="codecs">The codecs to match.</param>
    /// <param name="container">The container to match.</param>
    /// <param name="useSubContainer">Consider sub-containers.</param>
    /// <returns>True if both conditions are met.</returns>
    public bool ContainsAnyCodec(IReadOnlyList<string> codecs, string? container, bool useSubContainer = false)
    {
        var containerToCheck = useSubContainer && string.Equals(Container, "hls", StringComparison.OrdinalIgnoreCase) ? SubContainer : Container;
        return ContainerHelper.ContainsContainer(containerToCheck, container) && codecs.Any(c => ContainerHelper.ContainsContainer(Codec, false, c));
    }

    /// <summary>
    /// Checks to see whether the codecs and containers contain the given parameters.
    /// </summary>
    /// <param name="codec">The codec to match.</param>
    /// <param name="container">The container to match.</param>
    /// <param name="useSubContainer">Consider sub-containers.</param>
    /// <returns>True if both conditions are met.</returns>
    public bool ContainsAnyCodec(string? codec, string? container, bool useSubContainer = false)
    {
        return ContainsAnyCodec(codec.AsSpan(), container, useSubContainer);
    }

    /// <summary>
    /// Checks to see whether the codecs and containers contain the given parameters.
    /// </summary>
    /// <param name="codec">The codec to match.</param>
    /// <param name="container">The container to match.</param>
    /// <param name="useSubContainer">Consider sub-containers.</param>
    /// <returns>True if both conditions are met.</returns>
    public bool ContainsAnyCodec(ReadOnlySpan<char> codec, string? container, bool useSubContainer = false)
    {
        var containerToCheck = useSubContainer && string.Equals(Container, "hls", StringComparison.OrdinalIgnoreCase) ? SubContainer : Container;
        return ContainerHelper.ContainsContainer(containerToCheck, container) && ContainerHelper.ContainsContainer(Codec, false, codec);
    }
}
