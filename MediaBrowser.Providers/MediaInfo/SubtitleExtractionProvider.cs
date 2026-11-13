using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.MediaInfo;

/// <summary>
/// Extracts embedded subtitles into the subtitle cache during library scans, so playback
/// does not have to wait for a full read of the video file on the first subtitle request.
/// </summary>
public class SubtitleExtractionProvider : ICustomMetadataProvider<Episode>,
    ICustomMetadataProvider<MusicVideo>,
    ICustomMetadataProvider<Movie>,
    ICustomMetadataProvider<Trailer>,
    ICustomMetadataProvider<Video>,
    IHasItemChangeMonitor,
    IHasOrder,
    IForcedProvider
{
    private readonly ILibraryManager _libraryManager;
    private readonly ISubtitleEncoder _subtitleEncoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleExtractionProvider"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="subtitleEncoder">The subtitle encoder.</param>
    public SubtitleExtractionProvider(
        ILibraryManager libraryManager,
        ISubtitleEncoder subtitleEncoder)
    {
        _libraryManager = libraryManager;
        _subtitleEncoder = subtitleEncoder;
    }

    /// <inheritdoc />
    public string Name => "Subtitle Extraction Provider";

    /// <inheritdoc />
    /// <remarks>Runs after the <see cref="ProbeProvider"/>, which saves the media streams this provider relies on.</remarks>
    public int Order => 110;

    /// <inheritdoc />
    public bool HasChanged(BaseItem item, IDirectoryService directoryService)
    {
        if (item.IsFileProtocol)
        {
            var file = directoryService.GetFile(item.Path);
            if (file is not null && item.HasChanged(file.LastWriteTimeUtc))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(MusicVideo item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Trailer item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ItemUpdateType> FetchAsync(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, cancellationToken);
    }

    /// <summary>
    /// Extracts all extractable embedded subtitles of a video into the subtitle cache.
    /// Streams whose cached copy is still fresh are skipped by the subtitle encoder.
    /// </summary>
    /// <param name="video">The video.</param>
    /// <param name="subtitleEncoder">The subtitle encoder.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the extraction.</returns>
    internal static async Task ExtractSubtitlesAsync(Video video, ISubtitleEncoder subtitleEncoder, CancellationToken cancellationToken)
    {
        // Extraction reads the whole file, so limit it to local video files. Disc structures,
        // shortcuts and remote streams are left to on-demand extraction.
        if (!video.IsFileProtocol || video.IsShortcut || video.VideoType != VideoType.VideoFile)
        {
            return;
        }

        // Alternate versions are separate items and handle their own files.
        var mediaSourceId = video.Id.ToString("N", CultureInfo.InvariantCulture);
        var mediaSource = video.GetMediaSources(false)
            .FirstOrDefault(source => string.Equals(source.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase));

        if (mediaSource is null || !mediaSource.MediaStreams.Any(IsExtractableStream))
        {
            return;
        }

        await subtitleEncoder.ExtractAllExtractableSubtitles(mediaSource, cancellationToken).ConfigureAwait(false);
    }

    // Mirrors the stream selection in SubtitleEncoder.ExtractAllExtractableSubtitles.
    private static bool IsExtractableStream(MediaStream stream)
    {
        return stream is { IsExtractableSubtitleStream: true, SupportsExternalStream: true }
            && (!stream.IsExternal || stream.Path.EndsWith(".mks", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<ItemUpdateType> FetchInternal(Video video, CancellationToken cancellationToken)
    {
        var libraryOptions = _libraryManager.GetLibraryOptions(video);
        if (libraryOptions is null || !libraryOptions.ExtractSubtitlesDuringLibraryScan)
        {
            return ItemUpdateType.None;
        }

        // Blocking on purpose: running extractions in the background would start one full-file
        // read per new video at once, while the scan's own concurrency limit bounds this.
        await ExtractSubtitlesAsync(video, _subtitleEncoder, cancellationToken).ConfigureAwait(false);

        // Only the subtitle cache is written, the item itself is unchanged
        return ItemUpdateType.None;
    }
}
