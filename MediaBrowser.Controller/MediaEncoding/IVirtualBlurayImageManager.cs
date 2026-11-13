using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Exposes Blu-ray folders as virtual disc images.
/// </summary>
public interface IVirtualBlurayImageManager
{
    /// <summary>Sets the virtual ISO representation on an outgoing media source.</summary>
    /// <param name="source">The client-facing media source.</param>
    void ApplyTo(MediaSourceInfo source);

    /// <summary>Gets an image after validating the source file metadata.</summary>
    /// <param name="path">The disc folder or its BDMV directory.</param>
    /// <returns>The image, or null when the folder cannot be represented.</returns>
    IVirtualBlurayImage? GetImage(string path);
}
