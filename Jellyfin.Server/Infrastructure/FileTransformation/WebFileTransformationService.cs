using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.FileTransformation;

namespace Jellyfin.Server.Infrastructure.FileTransformation;

/// <summary>
/// Provides methods for Writing and Reading file Transformations.
/// </summary>
public class WebFileTransformationService : IWebFileTransformationReadService, IWebFileTransformationWriteService
{
    private readonly IDictionary<string, ICollection<TransformFile>> _fileTransformations;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebFileTransformationService"/> class.
    /// </summary>
    public WebFileTransformationService()
    {
        _fileTransformations = new Dictionary<string, ICollection<TransformFile>>();
    }

    /// <inheritdoc />
    public bool NeedsTransformation(string path)
    {
        return _fileTransformations.ContainsKey(path);
    }

    /// <inheritdoc />
    public void RunTransformation(string path, Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var pipeline = _fileTransformations[path];
        foreach (var action in pipeline)
        {
            stream.Seek(0, SeekOrigin.Begin);
            action(path, stream);
        }
    }

    /// <inheritdoc />
    public void AddTransformation(string path, TransformFile transformation)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (transformation == null)
        {
            throw new ArgumentNullException(nameof(transformation));
        }

        if (!_fileTransformations.TryGetValue(path, out var pipeline))
        {
            pipeline = new List<TransformFile>();
            _fileTransformations[path] = pipeline;
        }

        pipeline.Add(transformation);
    }
}
