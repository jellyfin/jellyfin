using System;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using PDFtoImage;
using SkiaSharp;

namespace Emby.Server.Implementations.Streaming;

/// <summary>
/// Stream provider for progressive book streams. Converts PDFs into a CBZ archive of rasterized images.
/// </summary>
public class CbzStreamProvider : IStreamProvider
{
    private readonly ILogger<CbzStreamProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CbzStreamProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public CbzStreamProvider(ILogger<CbzStreamProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string StreamContainer => "cbz";

    /// <inheritdoc />
    public MediaStreamProtocol StreamProtocol => MediaStreamProtocol.http;

    /// <inheritdoc />
    public bool Supports(BaseItem item, DeviceProfile profile)
        => MimeTypes.GetMimeType(item.Path) == MediaTypeNames.Application.Pdf && profile.TranscodingProfiles.Any(p => p.Type == DlnaProfileType.Book && string.Equals(p.Container, StreamContainer, StringComparison.Ordinal));

    /// <inheritdoc />
    public Task<Stream> Stream(MediaSourceInfo mediaSource, string playSessionId)
    {
        if (mediaSource == null || string.IsNullOrEmpty(mediaSource.Path))
        {
            throw new FileNotFoundException($"media source not found: {playSessionId}");
        }

        _logger.LogInformation("converting PDF to CBZ for play session {PlaySessionId}: {Path}", playSessionId, mediaSource.Path);
        var pipe = new Pipe();

        _ = WriteCbzAsync(mediaSource.Path, pipe.Writer, playSessionId);
        return Task.FromResult(pipe.Reader.AsStream());
    }

    private async Task WriteCbzAsync(string path, PipeWriter writer, string playSessionId)
    {
        var pageIndex = 0;

#pragma warning disable CA1416
        using (var archive = new ZipArchive(writer.AsStream(leaveOpen: true), ZipArchiveMode.Create))
        {
            using var pdfStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

            await foreach (var bitmap in Conversion.ToImagesAsync(pdfStream, leaveOpen: true).ConfigureAwait(false))
            {
                using (bitmap)
                {
                    var entry = archive.CreateEntry($"page-{pageIndex + 1:D4}.jpg", CompressionLevel.Optimal);

                    using var entryStream = await entry.OpenAsync();
                    using var pixmap = bitmap.PeekPixels();
                    using var encodedData = pixmap.Encode(SKEncodedImageFormat.Jpeg, 80) ?? throw new InvalidOperationException($"failed to encode page {pageIndex + 1} for session {playSessionId}");

                    await encodedData.AsStream().CopyToAsync(entryStream).ConfigureAwait(false);
                }

                pageIndex++;
            }
        }
#pragma warning restore CA1416

        _logger.LogDebug("converted PDF to archive for play session {PlaySessionId}", playSessionId);
        await writer.CompleteAsync().ConfigureAwait(false);
    }
}
