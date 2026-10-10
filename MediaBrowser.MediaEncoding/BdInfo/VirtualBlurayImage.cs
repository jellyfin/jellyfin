using System.IO;
using DiscUtils.Udf;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// An immutable image layout; each HTTP request opens an independent stream.
/// </summary>
public sealed class VirtualBlurayImage
{
    private readonly UdfBuilder _builder;
    private readonly object _lock = new();

    internal VirtualBlurayImage(UdfBuilder builder, string tag, int clipCount)
    {
        _builder = builder;
        Tag = tag;
        ClipCount = clipCount;
        using var image = Open();
        Length = image.Length;
    }

    /// <summary>Gets the metadata fingerprint of the source files.</summary>
    public string Tag { get; }

    /// <summary>Gets the number of clips in the main playlist.</summary>
    public int ClipCount { get; }

    /// <summary>Gets the total image length in bytes.</summary>
    public long Length { get; }

    /// <summary>Opens a new seekable stream without copying the source files.</summary>
    /// <returns>The image stream.</returns>
    public Stream Open()
    {
        lock (_lock)
        {
            return _builder.Build();
        }
    }
}
