using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Lyric Manager.
/// </summary>
public class LyricManager : ILyricManager
{
    private readonly ILogger<LyricManager> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly IMediaSourceManager _mediaSourceManager;

    private readonly ILyricProvider[] _lyricProviders;
    private readonly ILyricParser[] _lyricParsers;

    /// <summary>
    /// Initializes a new instance of the <see cref="LyricManager"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{LyricManager}"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryMonitor">Instance of the <see cref="ILibraryMonitor"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="lyricProviders">The list of <see cref="ILyricProvider"/>.</param>
    /// <param name="lyricParsers">The list of <see cref="ILyricParser"/>.</param>
    public LyricManager(
        ILogger<LyricManager> logger,
        IFileSystem fileSystem,
        ILibraryMonitor libraryMonitor,
        IMediaSourceManager mediaSourceManager,
        IEnumerable<ILyricProvider> lyricProviders,
        IEnumerable<ILyricParser> lyricParsers)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _libraryMonitor = libraryMonitor;
        _mediaSourceManager = mediaSourceManager;
        _lyricProviders = lyricProviders
            .OrderBy(i => i is IHasOrder hasOrder ? hasOrder.Order : 0)
            .ToArray();
        _lyricParsers = lyricParsers
            .OrderBy(l => l.Priority)
            .ToArray();
    }

    /// <inheritdoc />
    public event EventHandler<LyricDownloadFailureEventArgs>? LyricDownloadFailure;

    /// <inheritdoc />
    public Task<IReadOnlyList<RemoteLyricInfoDto>> SearchLyricsAsync(Audio audio, bool isAutomated, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);

        var request = new LyricSearchRequest
        {
            MediaPath = audio.Path,
            SongName = audio.Name,
            AlbumName = audio.Album,
            ArtistNames = audio.GetAllArtists().ToList(),
            Duration = audio.RunTimeTicks,
            IsAutomated = isAutomated
        };

        return SearchLyricsAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RemoteLyricInfoDto>> SearchLyricsAsync(LyricSearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var providers = _lyricProviders
            .Where(i => !request.DisabledLyricFetchers.Contains(i.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(i =>
            {
                var index = request.LyricFetcherOrder.IndexOf(i.Name);
                return index == -1 ? int.MaxValue : index;
            })
            .ToArray();

        // If not searching all, search one at a time until something is found
        if (!request.SearchAllProviders)
        {
            foreach (var provider in providers)
            {
                var providerResult = await InternalSearchProviderAsync(provider, request, cancellationToken).ConfigureAwait(false);
                if (providerResult.Count > 0)
                {
                    return providerResult;
                }
            }

            return [];
        }

        var tasks = providers.Select(async provider => await InternalSearchProviderAsync(provider, request, cancellationToken).ConfigureAwait(false));

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return results.SelectMany(i => i).ToArray();
    }

    /// <inheritdoc />
    public Task<LyricDto?> DownloadLyricsAsync(Audio audio, string lyricId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentException.ThrowIfNullOrWhiteSpace(lyricId);

        var libraryOptions = BaseItem.LibraryManager.GetLibraryOptions(audio);

        return DownloadLyricsAsync(audio, libraryOptions, lyricId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<LyricDto?> DownloadLyricsAsync(Audio audio, LibraryOptions libraryOptions, string lyricId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(libraryOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(lyricId);

        var provider = GetProvider(lyricId.AsSpan().LeftPart('_').ToString());
        if (provider is null)
        {
            return null;
        }

        try
        {
            var response = await InternalGetRemoteLyricsAsync(lyricId, cancellationToken).ConfigureAwait(false);
            if (response is null)
            {
                _logger.LogDebug("Unable to download lyrics for {LyricId}", lyricId);
                return null;
            }

            var parsedLyrics = await InternalParseRemoteLyricsAsync(response.Format, response.Stream, cancellationToken).ConfigureAwait(false);
            if (parsedLyrics is null)
            {
                return null;
            }

            await TrySaveLyric(audio, libraryOptions, response.Format, response.Stream).ConfigureAwait(false);
            return parsedLyrics;
        }
        catch (RateLimitExceededException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LyricDownloadFailure?.Invoke(this, new LyricDownloadFailureEventArgs
            {
                Item = audio,
                Exception = ex,
                Provider = provider.Name
            });

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LyricDto?> SaveLyricAsync(Audio audio, string format, string lyrics)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentException.ThrowIfNullOrEmpty(format);
        ArgumentException.ThrowIfNullOrEmpty(lyrics);

        var bytes = Encoding.UTF8.GetBytes(lyrics);
        using var lyricStream = new MemoryStream(bytes, 0, bytes.Length, false, true);
        return await SaveLyricAsync(audio, format, lyricStream).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LyricDto?> SaveLyricAsync(Audio audio, string format, Stream lyrics)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentException.ThrowIfNullOrEmpty(format);
        ArgumentNullException.ThrowIfNull(lyrics);

        var libraryOptions = BaseItem.LibraryManager.GetLibraryOptions(audio);

        var parsed = await InternalParseRemoteLyricsAsync(format, lyrics, CancellationToken.None).ConfigureAwait(false);
        if (parsed is null)
        {
            return null;
        }

        await TrySaveLyric(audio, libraryOptions, format, lyrics).ConfigureAwait(false);
        return parsed;
    }

    /// <inheritdoc />
    public async Task<LyricDto?> GetRemoteLyricsAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        var lyricResponse = await InternalGetRemoteLyricsAsync(id, cancellationToken).ConfigureAwait(false);
        if (lyricResponse is null)
        {
            return null;
        }

        return await InternalParseRemoteLyricsAsync(lyricResponse.Format, lyricResponse.Stream, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DeleteLyricsAsync(Audio audio)
    {
        ArgumentNullException.ThrowIfNull(audio);
        var streams = _mediaSourceManager.GetMediaStreams(new MediaStreamQuery
        {
            ItemId = audio.Id,
            Type = MediaStreamType.Lyric
        });

        foreach (var stream in streams)
        {
            var path = stream.Path;
            _libraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                _fileSystem.DeleteFile(path);
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }

        return audio.RefreshMetadata(CancellationToken.None);
    }

    /// <inheritdoc />
    public IReadOnlyList<LyricProviderInfo> GetSupportedProviders(BaseItem item)
    {
        if (item is not Audio)
        {
            return [];
        }

        return _lyricProviders.Select(p => new LyricProviderInfo { Name = p.Name, Id = GetProviderId(p.Name) }).ToList();
    }

    /// <inheritdoc />
    public async Task<LyricDto?> GetLyricsAsync(Audio audio, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);

        var lyricStreams = audio.GetMediaStreams().Where(s => s.Type == MediaStreamType.Lyric);
        foreach (var lyricStream in lyricStreams)
        {
            var lyricContents = await File.ReadAllTextAsync(lyricStream.Path, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

            var lyricFile = new LyricFile(Path.GetFileName(lyricStream.Path), lyricContents);
            foreach (var parser in _lyricParsers)
            {
                var parsedLyrics = parser.ParseLyrics(lyricFile);
                if (parsedLyrics is not null)
                {
                    return parsedLyrics;
                }
            }
        }

        return null;
    }

    private ILyricProvider? GetProvider(string providerId)
    {
        var provider = _lyricProviders.FirstOrDefault(p => string.Equals(providerId, GetProviderId(p.Name), StringComparison.Ordinal));
        if (provider is null)
        {
            _logger.LogWarning("Unknown provider id: {ProviderId}", providerId.ReplaceLineEndings(string.Empty));
        }

        return provider;
    }

    private string GetProviderId(string name)
        => name.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);

    private async Task<LyricDto?> InternalParseRemoteLyricsAsync(string format, Stream lyricStream, CancellationToken cancellationToken)
    {
        lyricStream.Seek(0, SeekOrigin.Begin);
        using var streamReader = new StreamReader(lyricStream, leaveOpen: true);
        var lyrics = await streamReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var lyricFile = new LyricFile($"lyric.{format}", lyrics);
        foreach (var parser in _lyricParsers)
        {
            var parsedLyrics = parser.ParseLyrics(lyricFile);
            if (parsedLyrics is not null)
            {
                return parsedLyrics;
            }
        }

        return null;
    }

    private async Task<LyricResponse?> InternalGetRemoteLyricsAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var parts = id.Split('_', 2);
        var provider = GetProvider(parts[0]);
        if (provider is null)
        {
            return null;
        }

        id = parts[^1];

        return await provider.GetLyricsAsync(id, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<RemoteLyricInfoDto>> InternalSearchProviderAsync(
        ILyricProvider provider,
        LyricSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var providerId = GetProviderId(provider.Name);
            var searchResults = await provider.SearchAsync(request, cancellationToken).ConfigureAwait(false);
            var parsedResults = new List<RemoteLyricInfoDto>();
            foreach (var result in searchResults)
            {
                var parsedLyrics = await InternalParseRemoteLyricsAsync(result.Lyrics.Format, result.Lyrics.Stream, cancellationToken).ConfigureAwait(false);
                if (parsedLyrics is null)
                {
                    continue;
                }

                parsedLyrics.Metadata = result.Metadata;
                parsedResults.Add(new RemoteLyricInfoDto
                {
                    Id = $"{providerId}_{result.Id}",
                    ProviderName = result.ProviderName,
                    Lyrics = parsedLyrics
                });
            }

            return parsedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading lyrics from {Provider}", provider.Name);
            return [];
        }
    }

    private async Task TrySaveLyric(
        Audio audio,
        LibraryOptions libraryOptions,
        string format,
        Stream lyricStream)
    {
        var saveInMediaFolder = libraryOptions.SaveLyricsWithMedia;

        var memoryStream = new MemoryStream();
        await using (memoryStream.ConfigureAwait(false))
        {
            await using (lyricStream.ConfigureAwait(false))
            {
                lyricStream.Seek(0, SeekOrigin.Begin);
                await lyricStream.CopyToAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Seek(0, SeekOrigin.Begin);
            }

            var savePaths = new List<string>();
            var saveFileName = Path.GetFileNameWithoutExtension(audio.Path) + "." + format.ReplaceLineEndings(string.Empty).ToLowerInvariant();

            if (saveInMediaFolder)
            {
                var mediaFolderPath = Path.GetFullPath(Path.Combine(audio.ContainingFolderPath, saveFileName));
                // TODO: Add some error handling to the API user: return BadRequest("Could not save lyric, bad path.");
                if (mediaFolderPath.StartsWith(audio.ContainingFolderPath, StringComparison.Ordinal))
                {
                    savePaths.Add(mediaFolderPath);
                }
            }

            var internalPath = Path.GetFullPath(Path.Combine(audio.GetInternalMetadataPath(), saveFileName));

            // TODO: Add some error to the user: return BadRequest("Could not save lyric, bad path.");
            if (internalPath.StartsWith(audio.GetInternalMetadataPath(), StringComparison.Ordinal))
            {
                savePaths.Add(internalPath);
            }

            if (savePaths.Count > 0)
            {
                await TrySaveToFiles(memoryStream, savePaths).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError("An uploaded lyric could not be saved because the resulting paths were invalid.");
            }
        }
    }

    private async Task TrySaveToFiles(Stream stream, List<string> savePaths)
    {
        List<Exception>? exs = null;

        foreach (var savePath in savePaths)
        {
            _logger.LogInformation("Saving lyrics to {SavePath}", savePath.ReplaceLineEndings(string.Empty));

            _libraryMonitor.ReportFileSystemChangeBeginning(savePath);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? throw new InvalidOperationException("Path can't be a root directory."));

                var fileOptions = AsyncFile.WriteOptions;
                fileOptions.Mode = FileMode.Create;
                fileOptions.PreallocationSize = stream.Length;
                var fs = new FileStream(savePath, fileOptions);
                await using (fs.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(fs).ConfigureAwait(false);
                }

                return;
            }
            catch (Exception ex)
            {
                (exs ??= []).Add(ex);
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(savePath, false);
            }

            stream.Position = 0;
        }

        if (exs is not null)
        {
            throw new AggregateException(exs);
        }
    }
}
