using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.MediaInfo;
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
    public string Name => nameof(CbzStreamProvider);

    /// <inheritdoc />
    public MediaStreamProtocol StreamProtocol => MediaStreamProtocol.http;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item.MediaType == MediaType.Book && Path.GetExtension(item.Path).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<Stream> Stream(PlaybackInfoResponse playbackInfo)
    {
        var path = playbackInfo.MediaSources.FirstOrDefault()?.Path;
        if (string.IsNullOrEmpty(path))
        {
            throw new FileNotFoundException($"media source not found: {playbackInfo.PlaySessionId}");
        }

        _logger.LogInformation("converting PDF to CBZ for play session {PlaySessionId}: {Path}", playbackInfo.PlaySessionId, path);

        var memoryStream = new MemoryStream();
        var pageIndex = 0;

#pragma warning disable CA1416
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var pdfStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

            await foreach (var bitmap in Conversion.ToImagesAsync(pdfStream, leaveOpen: true))
            {
                using (bitmap)
                {
                    var entry = archive.CreateEntry($"page-{pageIndex + 1:D4}.jpg", CompressionLevel.Optimal);

                    using var entryStream = await entry.OpenAsync();
                    using var pixmap = bitmap.PeekPixels();
                    using var encodedData = pixmap.Encode(SKEncodedImageFormat.Jpeg, 80) ?? throw new InvalidOperationException($"failed to encode page {pageIndex + 1} for session {playbackInfo.PlaySessionId}");

                    await encodedData.AsStream().CopyToAsync(entryStream).ConfigureAwait(false);
                }

                pageIndex++;
            }
        }
#pragma warning restore CA1416

        _logger.LogDebug("converted PDF to archive for play session {PlaySessionId}", playbackInfo.PlaySessionId);

        memoryStream.Position = 0;
        return memoryStream;
    }
}
