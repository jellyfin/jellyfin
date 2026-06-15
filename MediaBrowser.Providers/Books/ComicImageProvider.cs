using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;

namespace MediaBrowser.Providers.Books;

/// <summary>
/// The ComicImageProvider tries to find either an image named "cover" or, in case that
/// fails, just takes the first image inside the archive, hoping that it is the cover.
/// </summary>
public class ComicImageProvider : IDynamicImageProvider
{
    private readonly string[] _comicBookExtensions = [".cb7", ".cbr", ".cbt", ".cbz"];
    private readonly string[] _coverExtensions = [".png", ".jpeg", ".jpg", ".webp", ".bmp", ".gif"];

    private readonly ILogger<ComicImageProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComicImageProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{ComicImageProvider}"/> interface.</param>
    public ComicImageProvider(ILogger<ComicImageProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Comic Book Archive Cover Extractor";

    /// <inheritdoc />
    public async Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(item.Path);

        if (_comicBookExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return await LoadCoverAsync(item, cancellationToken).ConfigureAwait(false);
        }

        return new DynamicImageResponse { HasImage = false };
    }

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        return item is Book;
    }

    /// <summary>
    /// Tries to load a cover from the CBZ archive. Returns a response
    /// with no image if nothing is found.
    /// </summary>
    /// <param name="item">Item to check for covers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task<DynamicImageResponse> LoadCoverAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var memoryStream = new MemoryStream();

        try
        {
            ImageFormat imageFormat;

            using (Stream stream = AsyncFile.OpenRead(item.Path))
            {
                var archive = await ArchiveFactory.OpenAsyncArchive(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                await using (archive.ConfigureAwait(false))
                {
                    // throw exception to log results if no cover is found
                    (var cover, imageFormat) = await FindCoverEntryInArchiveAsync(archive).ConfigureAwait(false)
                        ?? throw new InvalidOperationException("no supported cover found");

                    // copy the cover to memory stream
                    var coverStream = await cover.OpenEntryStreamAsync(cancellationToken).ConfigureAwait(false);
                    await using (coverStream.ConfigureAwait(false))
                    {
                        await coverStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            // reset stream position after copying
            memoryStream.Position = 0;

            return new DynamicImageResponse { HasImage = true, Stream = memoryStream, Format = imageFormat };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to load cover from {Path}", item.Path);
            return new DynamicImageResponse { HasImage = false };
        }
    }

    /// <summary>
    /// Tries to find the entry containing the cover.
    /// </summary>
    /// <param name="archive">The archive to search.</param>
    /// <returns>The search result.</returns>
    private async ValueTask<(IArchiveEntry CoverEntry, ImageFormat ImageFormat)?> FindCoverEntryInArchiveAsync(IAsyncArchive archive)
    {
        IArchiveEntry? cover;

        // only some comics will explicitly name their cover file
        // in many cases the cover will simply be the first image in the archive
        foreach (var extension in _coverExtensions)
        {
            cover = await archive.EntriesAsync.FirstOrDefaultAsync(e => e.Key == "cover" + extension).ConfigureAwait(false);

            if (cover is not null)
            {
                var imageFormat = GetImageFormat(extension);

                return (cover, imageFormat);
            }
        }

        cover = await archive.EntriesAsync.OrderBy(x => x.Key)
            .FirstOrDefaultAsync(x => _coverExtensions.Contains(Path.GetExtension(x.Key), StringComparison.OrdinalIgnoreCase))
            .ConfigureAwait(false);

        if (cover is not null)
        {
            var imageFormat = GetImageFormat(Path.GetExtension(cover.Key ?? string.Empty));

            return (cover, imageFormat);
        }

        return null;
    }

    private static ImageFormat GetImageFormat(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" => ImageFormat.Jpg,
        ".jpeg" => ImageFormat.Jpg,
        ".png" => ImageFormat.Png,
        ".webp" => ImageFormat.Webp,
        ".bmp" => ImageFormat.Bmp,
        ".gif" => ImageFormat.Gif,
        ".svg" => ImageFormat.Svg,
        _ => throw new ArgumentException($"unsupported extension: {extension}"),
    };
}
