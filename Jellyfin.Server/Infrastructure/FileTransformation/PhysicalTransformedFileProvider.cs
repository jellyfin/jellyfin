using System.Buffers;
using System.IO;
using MediaBrowser.Controller.FileTransformation;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Embedded;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Server.Infrastructure.FileTransformation;

/// <summary>
/// Provides file contents modified by <see cref="IWebFileTransformationReadService"/>.
/// </summary>
public class PhysicalTransformedFileProvider : IFileProvider
{
    private readonly PhysicalFileProvider _parentProvider;
    private readonly IWebFileTransformationReadService _webFileTransformationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhysicalTransformedFileProvider"/> class based on the set parent provider.
    /// </summary>
    /// <param name="parentProvider">The parent provider.</param>
    /// <param name="webFileTransformationService">The <see cref="IWebFileTransformationReadService"/>.</param>
    public PhysicalTransformedFileProvider(
        PhysicalFileProvider parentProvider,
        IWebFileTransformationReadService webFileTransformationService)
    {
        _parentProvider = parentProvider;
        _webFileTransformationService = webFileTransformationService;
    }

    /// <inheritdoc />
    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return _parentProvider.GetDirectoryContents(subpath);
    }

    /// <inheritdoc />
    public IFileInfo GetFileInfo(string subpath)
    {
        var iFileInfo = _parentProvider.GetFileInfo(subpath);
        if (iFileInfo is not PhysicalFileInfo { Exists: true } physicalFileInfo
            || !_webFileTransformationService.NeedsTransformation(subpath))
        {
            return iFileInfo;
        }

        using var sourceStream = physicalFileInfo.CreateReadStream();
        var transformedStream = new MemoryStream();
        sourceStream.CopyTo(transformedStream);
        transformedStream.Seek(0, SeekOrigin.Begin);

        _webFileTransformationService.RunTransformation(subpath, transformedStream);
        transformedStream.Seek(0, SeekOrigin.Begin);

        return new TransformableFileInfo(physicalFileInfo, transformedStream);
    }

    /// <inheritdoc />
    public IChangeToken Watch(string filter)
    {
        return _parentProvider.Watch(filter);
    }
}
