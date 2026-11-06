using System.IO;
using System.Threading;
using DiscUtils.Udf;
using MediaBrowser.Controller.MediaEncoding;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// An immutable image layout; each HTTP request opens an independent stream.
/// </summary>
public sealed class VirtualBlurayImage : IVirtualBlurayImage
{
    private readonly UdfBuilder _builder;
    private readonly Lock _lock = new();

    internal VirtualBlurayImage(UdfBuilder builder, string tag)
    {
        _builder = builder;
        Tag = tag;
        using var image = Open();
        Length = image.Length;
    }

    /// <summary>Gets the metadata fingerprint of the source files.</summary>
    public string Tag { get; }

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
