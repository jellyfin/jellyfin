using System.IO;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// A virtual disc image that opens independent seekable streams.
/// </summary>
public interface IVirtualBlurayImage
{
    /// <summary>Gets the metadata fingerprint of the source files.</summary>
    string Tag { get; }

    /// <summary>Gets the total image length in bytes.</summary>
    long Length { get; }

    /// <summary>Opens a new seekable stream without copying the source files.</summary>
    /// <returns>The image stream.</returns>
    Stream Open();
}
