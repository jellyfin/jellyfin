using System;
using System.Collections.Frozen;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Definitions.Licensing;
using MimeDetective.Engine;
using MimeDetective.Storage;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Utility class providing upload helper functions.
/// </summary>
public class UploadHelper
{
    private readonly FrozenSet<Definition> _videoDefinitions;
    private readonly FrozenSet<Definition> _audioDefinitions;
    private readonly FrozenSet<Definition> _imageDefinitions;
    private readonly ILogger<UploadHelper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadHelper"/> class.
    /// </summary>
    /// <param name="namingOptions">The naming options.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{UploadHelper}"/> interface.</param>
    public UploadHelper(
        NamingOptions namingOptions,
        ILogger<UploadHelper> logger)
    {
        var allDefinitions = new ExhaustiveBuilder()
            {
                UsageType = UsageType.PersonalNonCommercial
            }.Build();

        var extensions = namingOptions.AudioFileExtensions.Select(x => x.Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase)).ToArray();
        _audioDefinitions = allDefinitions
            .ScopeExtensions(extensions)
            .TrimMeta()
            .TrimDescription()
            .ToFrozenSet();

        extensions = namingOptions.VideoFileExtensions.Select(x => x.Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase)).ToArray();
        _videoDefinitions = allDefinitions
            .ScopeExtensions(extensions)
            .TrimMeta()
            .TrimDescription()
            .ToFrozenSet();

        extensions =
            [
                "jpg",
                "png",
                "gif",
                "webp",
                "bmp"
            ];
        _imageDefinitions = allDefinitions
            .ScopeExtensions(extensions)
            .TrimMeta()
            .TrimDescription()
            .ToFrozenSet();

        _logger = logger;
    }

    /// <summary>
    /// Checks if data MIME type matches content type and returns the MIME type information.
    /// </summary>
    /// <param name="stream">The data stream.</param>
    /// <param name="contentType">The content type.</param>
    /// <returns>MIME type information.</returns>
    public DefinitionMatch? GetMimeInfo(Stream? stream, string? contentType)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(contentType);

        var definitions = GetDefinitionsForType(contentType.Split('/')[0]);
        var inspector = new ContentInspectorBuilder()
        {
            Definitions = [.. definitions],
        }.Build();

        var realMimeTypeMatchesContentType = inspector.Inspect(stream)
            .Where(r => string.Equals(r.Definition.File.MimeType, contentType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Points)
            .FirstOrDefault(r => r.Type == DefinitionMatchType.Complete);
        if (realMimeTypeMatchesContentType is not null)
        {
            return realMimeTypeMatchesContentType;
        }

        return null;
    }

    /// <summary>
    /// Writes the stream content to a file.
    /// </summary>
    /// <param name="stream">The data stream.</param>
    /// <param name="filePath">The file path to write the data to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> writing the stream to a file.</returns>
    public static async Task WriteStreamToFile(Stream stream, string filePath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(filePath);

        var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
        await using (fs.ConfigureAwait(false))
        {
            await stream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
    }

    private FrozenSet<Definition> GetDefinitionsForType(string type)
    {
        return type switch
        {
            "audio" => _audioDefinitions,
            "video" => _videoDefinitions,
            "image" => _imageDefinitions,
            _ => FrozenSet<Definition>.Empty
        };
    }
}
