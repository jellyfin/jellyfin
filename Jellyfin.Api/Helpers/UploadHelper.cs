using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using Jellyfin.Extensions;
using MediaBrowser.Model.IO;
using MimeDetective;
using MimeDetective.Definitions;
using MimeDetective.Definitions.Licensing;
using MimeDetective.Engine;
using MimeDetective.Storage;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Utitlity class providing upload helper functions.
/// </summary>
public class UploadHelper
{
    /// <summary>
    /// Maximum number of bytes to read from the start of an upload for MIME sniffing.
    /// </summary>
    public const int MaxSniffBytes = 8192;

    private readonly List<Definition> _videoDefinitions;
    private readonly List<Definition> _audioDefinitions;
    private readonly List<Definition> _imageDefinitions;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadHelper"/> class.
    /// </summary>
    /// <param name="namingOptions">The naming options.</param>
    public UploadHelper(
        NamingOptions namingOptions)
    {
        var allDefinitions = new ExhaustiveBuilder()
            {
                UsageType = UsageType.PersonalNonCommercial
            }.Build();

        var extensions = namingOptions.AudioFileExtensions.Select(x => x.Replace(".", string.Empty, StringComparison.Ordinal)).ToArray();
        _audioDefinitions = allDefinitions
            .ScopeExtensions(extensions)
            .TrimMeta()
            .TrimDescription()
            .ToList();

        extensions = namingOptions.VideoFileExtensions.Select(x => x.Replace(".", string.Empty, StringComparison.Ordinal)).ToArray();
        _videoDefinitions = allDefinitions
            .ScopeExtensions(extensions)
            .TrimMeta()
            .TrimDescription()
            .ToList();

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
            .ToList();
    }

    /// <summary>
    /// Checks if the declared content type matches the bytes of the upload prefix and returns the matching definition.
    /// </summary>
    /// <param name="prefix">A prefix of the upload payload, typically the first <see cref="MaxSniffBytes"/> bytes.</param>
    /// <param name="contentType">The declared content type, optionally followed by parameters (e.g. <c>image/png; charset=utf-8</c>).</param>
    /// <returns>The matching definition, or <see langword="null"/> if the content type is unknown or does not match the bytes.</returns>
    public DefinitionMatch? GetMimeInfo(ArraySegment<byte> prefix, string? contentType)
    {
        ArgumentNullException.ThrowIfNull(contentType);

        var mediaType = contentType.AsSpan().LeftPart(';').Trim().ToString();
        var slash = mediaType.IndexOf('/', StringComparison.Ordinal);
        if (slash <= 0)
        {
            return null;
        }

        var definitions = GetDefinitionsForType(mediaType[..slash]);
        if (definitions is null)
        {
            return null;
        }

        using var stream = new MemoryStream(prefix.Array ?? [], prefix.Offset, prefix.Count, writable: false);
        var inspector = new ContentInspectorBuilder()
        {
            Definitions = definitions,
        }.Build();

        return inspector.Inspect(stream)
            .Where(r => string.Equals(r.Definition.File.MimeType, mediaType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Points)
            .FirstOrDefault(r => r.Type == DefinitionMatchType.Complete);
    }

    /// <summary>
    /// Writes the stream content to a file.
    /// </summary>
    /// <param name="stream">The data stream.</param>
    /// <param name="filePath">The file path to write the data to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
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

    private List<Definition>? GetDefinitionsForType(string type)
    {
        return type switch
        {
            "audio" => _audioDefinitions,
            "video" => _videoDefinitions,
            "image" => _imageDefinitions,
            _ => null,
        };
    }
}
