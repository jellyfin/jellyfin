using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
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
    /// The number of bytes inspected to determine the format of a file.
    /// Every format recognized here is identified by a header at the very start of the file.
    /// </summary>
    public const int MaxSniffBytes = 8192;

    /// <summary>
    /// The formats that may be served straight out of a media file, mapped to the MIME type to serve them as.
    /// A media file declares the MIME type of its own attachments, so that value is controlled by whoever
    /// created the file and must never reach the response. The format is detected from the content instead
    /// and the MIME type is taken from this table, which holds nothing a browser will execute in our origin.
    /// </summary>
    private static readonly FrozenDictionary<string, string> _attachmentMimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "bmp", MediaTypeNames.Image.Bmp },
        { "gif", MediaTypeNames.Image.Gif },
        { "jpeg", MediaTypeNames.Image.Jpeg },
        { "jpg", MediaTypeNames.Image.Jpeg },
        { "otc", MediaTypeNames.Font.Collection },
        { "otf", MediaTypeNames.Font.Otf },
        { "png", MediaTypeNames.Image.Png },
        { "ttc", MediaTypeNames.Font.Collection },
        { "ttf", MediaTypeNames.Font.Ttf },
        { "webp", MediaTypeNames.Image.Webp },
        { "woff", MediaTypeNames.Font.Woff },
        { "woff2", MediaTypeNames.Font.Woff2 }
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly FrozenSet<Definition> _videoDefinitions;
    private readonly FrozenSet<Definition> _audioDefinitions;
    private readonly FrozenSet<Definition> _imageDefinitions;
    private readonly IContentInspector _attachmentInspector;

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

        _attachmentInspector = new ContentInspectorBuilder()
        {
            Definitions = allDefinitions
                .ScopeExtensions(_attachmentMimeTypes.Keys)
                .TrimMeta()
                .TrimDescription()
                .ToList(),
        }.Build();
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
            Definitions = definitions.ToList(),
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
    /// Determines the MIME type an attachment embedded in a media file may be served with.
    /// </summary>
    /// <param name="stream">The attachment data stream, positioned at the start of the attachment.</param>
    /// <returns>
    /// The MIME type matching the detected format, or <c>application/octet-stream</c> if the format is not
    /// one that is safe to hand to a browser. The stream is left at the position it was passed in at.
    /// </returns>
    public string GetAttachmentMimeType(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanSeek)
        {
            return MediaTypeNames.Application.Octet;
        }

        var position = stream.Position;
        DefinitionMatch? match;
        try
        {
            // Inspecting reads to the end of the stream, so only hand it the header the formats are recognized by.
            var prefix = new byte[MaxSniffBytes];
            var read = stream.ReadAtLeast(prefix, prefix.Length, throwOnEndOfStream: false);
            using var prefixStream = new MemoryStream(prefix, 0, read, writable: false);
            match = _attachmentInspector.Inspect(prefixStream)
                .Where(r => r.Type == DefinitionMatchType.Complete)
                .MaxBy(r => r.Points);
        }
        finally
        {
            stream.Position = position;
        }

        if (match is not null)
        {
            foreach (var extension in match.Definition.File.Extensions)
            {
                if (_attachmentMimeTypes.TryGetValue(extension, out var mimeType))
                {
                    return mimeType;
                }
            }
        }

        return MediaTypeNames.Application.Octet;
    }

    /// <summary>
    /// Writes the stream content to a file.
    /// </summary>
    /// <param name="stream">The data stream.</param>
    /// <param name="filePath">The file path to write the data to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async void WriteStreamToFile(Stream stream, string filePath, CancellationToken cancellationToken)
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
            _ => FrozenSet<Definition>.Empty,
        };
    }
}
