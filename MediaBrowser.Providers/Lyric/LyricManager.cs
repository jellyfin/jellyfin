using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    public Task<RemoteLyricInfo[]> SearchLyricsAsync(Audio audio, bool isAutomated, CancellationToken cancellationToken)
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
    public async Task<RemoteLyricInfo[]> SearchLyricsAsync(LyricSearchRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var providers = _lyricProviders
            .Where(i => !request.DisabledLyricFetchers.Contains(i.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(i =>
            {
                var index = request.LyricFetcherOrder.ToList().IndexOf(i.Name);
                return index == -1 ? int.MaxValue : index;
            })
            .ToArray();

        // If not searching all, search one at a time until something is found
        if (!request.SearchAllProviders)
        {
            foreach (var provider in providers)
            {
                try
                {
                    var searchResults = await provider.SearchAsync(request, cancellationToken).ConfigureAwait(false);

                    var list = searchResults.ToArray();

                    if (list.Length > 0)
                    {
                        Normalize(list);
                        return list;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading lyrics from {Provider}", provider.Name);
                }
            }

            return [];
        }

        var tasks = providers.Select(async i =>
        {
            try
            {
                var searchResults = await i.SearchAsync(request, cancellationToken).ConfigureAwait(false);

                var list = searchResults.ToArray();
                Normalize(list);
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading lyrics from {0}", i.Name);
                return [];
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        return results.SelectMany(i => i).ToArray();
    }

    /// <inheritdoc />
    public Task DownloadLyricsAsync(Audio audio, string lyricId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentException.ThrowIfNullOrWhiteSpace(lyricId);

        var libraryOptions = BaseItem.LibraryManager.GetLibraryOptions(audio);

        return DownloadLyricsAsync(audio, libraryOptions, lyricId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DownloadLyricsAsync(Audio audio, LibraryOptions libraryOptions, string lyricId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(libraryOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(lyricId);

        var parts = lyricId.Split('_', 2);
        var provider = GetProvider(parts[0]);

        try
        {
            var response = await GetRemoteLyricsAsync(lyricId, cancellationToken).ConfigureAwait(false);
            await TrySaveLyric(audio, libraryOptions, response).ConfigureAwait(false);
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
    public Task UploadLyricAsync(Audio audio, LyricResponse lyricResponse)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(lyricResponse);
        var libraryOptions = BaseItem.LibraryManager.GetLibraryOptions(audio);
        return TrySaveLyric(audio, libraryOptions, lyricResponse);
    }

    /// <inheritdoc />
    public Task<LyricResponse> GetRemoteLyricsAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var parts = id.Split('_', 2);
        var provider = GetProvider(parts[0]);
        id = parts[^1];

        return provider.GetLyricsAsync(id, cancellationToken);
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
    public async Task<LyricModel?> GetLyricsAsync(Audio audio, CancellationToken cancellationToken)
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

    private ILyricProvider GetProvider(string providerId)
        => _lyricProviders.First(p => string.Equals(providerId, GetProviderId(p.Name), StringComparison.Ordinal));

    private string GetProviderId(string name)
        => name.ToLowerInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);

    private void Normalize(IEnumerable<RemoteLyricInfo> lyricInfos)
    {
        foreach (var lyric in lyricInfos)
        {
            lyric.Id = $"{GetProviderId(lyric.ProviderName)}_{lyric.Id}";
        }
    }

    private async Task TrySaveLyric(
        Audio audio,
        LibraryOptions libraryOptions,
        LyricResponse lyricResponse)
    {
        var saveInMediaFolder = libraryOptions.SaveLyricsWithMedia;

        var memoryStream = new MemoryStream();
        await using (memoryStream.ConfigureAwait(false))
        {
            var stream = lyricResponse.Stream;

            await using (stream.ConfigureAwait(false))
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                memoryStream.Seek(0, SeekOrigin.Begin);
            }

            var savePaths = new List<string>();
            var saveFileName = Path.GetFileNameWithoutExtension(audio.Path) + "." + lyricResponse.Format.ToLowerInvariant();

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
            _logger.LogInformation("Saving lyrics to {SavePath}", savePath);

            _libraryMonitor.ReportFileSystemChangeBeginning(savePath);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? throw new InvalidOperationException("Path can't be a root directory."));

                var fileOptions = AsyncFile.WriteOptions;
                fileOptions.Mode = FileMode.CreateNew;
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
