using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Music;
using MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using MetaBrainz.MusicBrainz.Interfaces.Searches;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// Music album metadata provider for MusicBrainz.
/// </summary>
public class MusicBrainzAlbumProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>, IHasOrder, IDisposable
{
    private readonly ILogger<MusicBrainzAlbumProvider> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private Query _musicBrainzQuery;

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicBrainzAlbumProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="providerManager">The provider manager.</param>
    /// <param name="fileSystem">The file system.</param>
    public MusicBrainzAlbumProvider(
        ILogger<MusicBrainzAlbumProvider> logger,
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _musicBrainzQuery = new Query();
        ReloadConfig(null, MusicBrainz.Plugin.Instance!.Configuration);
        MusicBrainz.Plugin.Instance!.ConfigurationChanged += ReloadConfig;
    }

    /// <inheritdoc />
    public string Name => "MusicBrainz";

    /// <inheritdoc />
    public int Order => 0;

    private void ReloadConfig(object? sender, BasePluginConfiguration e)
    {
        var configuration = (PluginConfiguration)e;
        if (Uri.TryCreate(configuration.Server, UriKind.Absolute, out var server))
        {
            Query.DefaultServer = server.DnsSafeHost;
            Query.DefaultPort = server.Port;
            Query.DefaultUrlScheme = server.Scheme;
        }
        else
        {
            // Fallback to official server
            _logger.LogWarning("Invalid MusicBrainz server specified, falling back to official server");
            var defaultServer = new Uri(PluginConfiguration.DefaultServer);
            Query.DefaultServer = defaultServer.Host;
            Query.DefaultPort = defaultServer.Port;
            Query.DefaultUrlScheme = defaultServer.Scheme;
        }

        Query.DelayBetweenRequests = configuration.RateLimit;
        _musicBrainzQuery = new Query();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
    {
        var releaseId = searchInfo.GetReleaseId();
        var releaseGroupId = searchInfo.GetReleaseGroupId();

        if (!string.IsNullOrEmpty(releaseId))
        {
            var releaseResult = await _musicBrainzQuery.LookupReleaseAsync(new Guid(releaseId), Include.Artists | Include.ReleaseGroups, cancellationToken).ConfigureAwait(false);
            return GetReleaseResult(releaseResult).SingleItemAsEnumerable();
        }

        if (!string.IsNullOrEmpty(releaseGroupId))
        {
            var releaseGroupResult = await _musicBrainzQuery.LookupReleaseGroupAsync(new Guid(releaseGroupId), Include.Releases, null, cancellationToken).ConfigureAwait(false);

            // No need to pass the cancellation token to GetReleaseGroupResultAsync as we're already passing it to ToBlockingEnumerable
            return GetReleaseGroupResultAsync(releaseGroupResult.Releases, CancellationToken.None).ToBlockingEnumerable(cancellationToken);
        }

        var artistMusicBrainzId = searchInfo.GetMusicBrainzArtistId();

        if (!string.IsNullOrWhiteSpace(artistMusicBrainzId))
        {
            var releaseSearchResults = await _musicBrainzQuery.FindReleasesAsync($"\"{searchInfo.Name}\" AND arid:{artistMusicBrainzId}", null, null, false, cancellationToken)
                .ConfigureAwait(false);

            if (releaseSearchResults.Results.Count > 0)
            {
                return GetReleaseSearchResultAsync(releaseSearchResults.Results, CancellationToken.None).ToBlockingEnumerable(cancellationToken);
            }
        }
        else
        {
            // I'm sure there is a better way but for now it resolves search for 12" Mixes
            var queryName = searchInfo.Name.Replace("\"", string.Empty, StringComparison.Ordinal);

            var releaseSearchResults = await _musicBrainzQuery.FindReleasesAsync($"\"{queryName}\" AND artist:\"{searchInfo.GetAlbumArtist()}\"c", null, null, false, cancellationToken)
                .ConfigureAwait(false);

            if (releaseSearchResults.Results.Count > 0)
            {
                return GetReleaseSearchResultAsync(releaseSearchResults.Results, CancellationToken.None).ToBlockingEnumerable(cancellationToken);
            }
        }

        return Enumerable.Empty<RemoteSearchResult>();
    }

    private async IAsyncEnumerable<RemoteSearchResult> GetReleaseSearchResultAsync(IEnumerable<ISearchResult<IRelease>>? releaseSearchResults, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (releaseSearchResults is null)
        {
            yield break;
        }

        foreach (var result in releaseSearchResults)
        {
            // Fetch full release info so the release group id is populated and gets returned to the Identify UI
            var fullResult = await _musicBrainzQuery.LookupReleaseAsync(result.Item.Id, Include.Artists | Include.ReleaseGroups, cancellationToken).ConfigureAwait(false);
            yield return GetReleaseResult(fullResult);
        }
    }

    private async IAsyncEnumerable<RemoteSearchResult> GetReleaseGroupResultAsync(IEnumerable<IRelease>? releaseSearchResults, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (releaseSearchResults is null)
        {
            yield break;
        }

        foreach (var result in releaseSearchResults)
        {
            // Fetch full release info, otherwise artists are missing
            var fullResult = await _musicBrainzQuery.LookupReleaseAsync(result.Id, Include.Artists | Include.ReleaseGroups, cancellationToken).ConfigureAwait(false);
            yield return GetReleaseResult(fullResult);
        }
    }

    private RemoteSearchResult GetReleaseResult(IRelease releaseSearchResult)
    {
        var searchResult = new RemoteSearchResult
        {
            Name = releaseSearchResult.Title,
            ProductionYear = releaseSearchResult.Date?.Year,
            PremiereDate = releaseSearchResult.Date?.NearestDate,
            SearchProviderName = Name
        };

        // Add artists and use first as album artist
        var artists = releaseSearchResult.ArtistCredit;
        if (artists is not null && artists.Count > 0)
        {
            var artistResults = new RemoteSearchResult[artists.Count];
            for (int i = 0; i < artists.Count; i++)
            {
                var artist = artists[i];
                var artistResult = new RemoteSearchResult
                {
                    Name = artist.Name
                };

                if (artist.Artist?.Id is not null)
                {
                    artistResult.SetProviderId(MetadataProvider.MusicBrainzArtist, artist.Artist!.Id.ToString());
                }

                artistResults[i] = artistResult;
            }

            searchResult.AlbumArtist = artistResults[0];
            searchResult.Artists = artistResults;
        }

        searchResult.SetProviderId(MetadataProvider.MusicBrainzAlbum, releaseSearchResult.Id.ToString());

        if (releaseSearchResult.ReleaseGroup?.Id is not null)
        {
            searchResult.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, releaseSearchResult.ReleaseGroup.Id.ToString());
        }

        return searchResult;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
    {
        var releaseId = info.GetReleaseId();
        var releaseGroupId = info.GetReleaseGroupId();

        var result = new MetadataResult<MusicAlbum>
        {
            Item = new MusicAlbum()
        };

        // If there is a release group, but no release ID, take the first release in the group
        if (string.IsNullOrWhiteSpace(releaseId) && !string.IsNullOrWhiteSpace(releaseGroupId))
        {
            var releaseGroup = await _musicBrainzQuery.LookupReleaseGroupAsync(new Guid(releaseGroupId), Include.Releases, null, cancellationToken).ConfigureAwait(false);
            var release = releaseGroup.Releases?.Count > 0 ? releaseGroup.Releases[0] : null;
            if (release is not null)
            {
                releaseId = release.Id.ToString();
            }
        }

        // If we still don't have a release ID, search for one
        if (string.IsNullOrWhiteSpace(releaseId))
        {
            var artistMusicBrainzId = info.GetMusicBrainzArtistId();
            IRelease? releaseResult = null;

            if (!string.IsNullOrEmpty(artistMusicBrainzId))
            {
                var releaseSearchResults = await _musicBrainzQuery.FindReleasesAsync($"\"{info.Name}\" AND arid:{artistMusicBrainzId}", null, null, false, cancellationToken)
                    .ConfigureAwait(false);
                releaseResult = releaseSearchResults.Results.Count > 0 ? releaseSearchResults.Results[0].Item : null;
            }
            else if (!string.IsNullOrEmpty(info.GetAlbumArtist()))
            {
                var releaseSearchResults = await _musicBrainzQuery.FindReleasesAsync($"\"{info.Name}\" AND artist:{info.GetAlbumArtist()}", null, null, false, cancellationToken)
                    .ConfigureAwait(false);
                releaseResult = releaseSearchResults.Results.Count > 0 ? releaseSearchResults.Results[0].Item : null;
            }

            if (releaseResult is not null)
            {
                releaseId = releaseResult.Id.ToString();
            }
        }

        // Once we have a release ID, fetch the full release and populate metadata
        if (!string.IsNullOrWhiteSpace(releaseId))
        {
            var release = await _musicBrainzQuery.LookupReleaseAsync(new Guid(releaseId), Include.ReleaseGroups | Include.ArtistCredits | Include.Recordings, cancellationToken).ConfigureAwait(false);

            result.HasMetadata = true;
            result.Item.Name = release.Title;
            result.Item.ProductionYear = release.Date?.Year;
            result.Item.PremiereDate = release.Date?.NearestDate;
            result.Item.Overview = release.Annotation;

            if (release.ArtistCredit is { Count: > 0 })
            {
                result.Item.AlbumArtists = release.ArtistCredit.Select(a => a.Name).ToArray();
            }

            if (string.IsNullOrWhiteSpace(releaseGroupId) && release.ReleaseGroup?.Id is not null)
            {
                releaseGroupId = release.ReleaseGroup.Id.ToString();
            }

            result.Item.SetProviderId(MetadataProvider.MusicBrainzAlbum, releaseId);

            if (!string.IsNullOrEmpty(releaseGroupId))
            {
                result.Item.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, releaseGroupId);
            }

            await PropagateMusicBrainzIdsToChildrenAsync(info, release, releaseId, releaseGroupId, cancellationToken).ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(releaseGroupId))
        {
            result.HasMetadata = true;
            result.Item.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, releaseGroupId);
        }

        return result;
    }

    private async Task PropagateMusicBrainzIdsToChildrenAsync(AlbumInfo info, IRelease release, string releaseId, string? releaseGroupId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(info.Path) || release.Media is null)
        {
            return;
        }

        if (_libraryManager.FindByPath(info.Path, true) is not MusicAlbum album)
        {
            return;
        }

        // Flatten release tracks once. A release may have multiple media (discs); we match across all.
        var releaseTracks = release.Media
            .Where(m => m.Tracks is not null)
            .SelectMany(m => m.Tracks!)
            .Where(t => !string.IsNullOrEmpty(t.Title))
            .ToList();

        if (releaseTracks.Count == 0)
        {
            return;
        }

        var albumArtistId = release.ArtistCredit?.FirstOrDefault()?.Artist?.Id.ToString();

        foreach (var audio in album.Tracks)
        {
            if (string.IsNullOrEmpty(audio.Name))
            {
                continue;
            }

            var changed = false;

            // Album-level ids are the same for every track in the release.
            if (TrySetProviderId(audio, MetadataProvider.MusicBrainzAlbum, releaseId))
            {
                changed = true;
            }

            if (!string.IsNullOrEmpty(releaseGroupId) && TrySetProviderId(audio, MetadataProvider.MusicBrainzReleaseGroup, releaseGroupId))
            {
                changed = true;
            }

            if (!string.IsNullOrEmpty(albumArtistId) && TrySetProviderId(audio, MetadataProvider.MusicBrainzAlbumArtist, albumArtistId))
            {
                changed = true;
            }

            // Track-level ids: trust the title match as the source of truth.
            var match = releaseTracks.FirstOrDefault(t => string.Equals(t.Title, audio.Name, StringComparison.OrdinalIgnoreCase));
            if (match?.Recording is not null)
            {
                if (TrySetProviderId(audio, MetadataProvider.MusicBrainzRecording, match.Recording.Id.ToString()))
                {
                    changed = true;
                }

                if (TrySetProviderId(audio, MetadataProvider.MusicBrainzTrack, match.Id.ToString()))
                {
                    changed = true;
                }
            }
            else
            {
                _logger.LogDebug("MusicBrainz propagate: no track in release {ReleaseId} matched audio title '{Audio}'", releaseId, audio.Name);
            }

            if (!changed)
            {
                continue;
            }

            await _libraryManager.UpdateItemAsync(audio, album, ItemUpdateType.MetadataDownload, cancellationToken).ConfigureAwait(false);

            // The album refresh runs children before itself, so the new ids would otherwise sit until the next manual refresh.
            _providerManager.QueueRefresh(
                audio.Id,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem)),
                RefreshPriority.High);
        }
    }

    private static bool TrySetProviderId(BaseItem item, MetadataProvider provider, string value)
    {
        if (string.Equals(item.GetProviderId(provider), value, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        item.SetProviderId(provider, value);
        return true;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose all resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _musicBrainzQuery.Dispose();
        }
    }
}
