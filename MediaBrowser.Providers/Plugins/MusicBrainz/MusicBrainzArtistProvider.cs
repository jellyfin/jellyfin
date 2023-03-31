using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
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
/// MusicBrainz artist provider.
/// </summary>
public class MusicBrainzArtistProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>, IDisposable
{
    private readonly ILogger<MusicBrainzArtistProvider> _logger;
    private Query _musicBrainzQuery;

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicBrainzArtistProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MusicBrainzArtistProvider(ILogger<MusicBrainzArtistProvider> logger)
    {
        _logger = logger;
        _musicBrainzQuery = new Query();
        ReloadConfig(null, MusicBrainz.Plugin.Instance!.Configuration);
        MusicBrainz.Plugin.Instance!.ConfigurationChanged += ReloadConfig;
    }

    /// <inheritdoc />
    public string Name => "MusicBrainz";

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
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        var artistId = searchInfo.GetMusicBrainzArtistId();

        if (!string.IsNullOrWhiteSpace(artistId))
        {
            var artistResult = await _musicBrainzQuery.LookupArtistAsync(new Guid(artistId), Include.Aliases, null, null, cancellationToken).ConfigureAwait(false);
            return GetResultFromResponse(artistResult).SingleItemAsEnumerable();
        }

        var artistSearchResults = await _musicBrainzQuery.FindArtistsAsync($"\"{searchInfo.Name}\"", null, null, false, cancellationToken)
            .ConfigureAwait(false);
        if (artistSearchResults.Results.Count > 0)
        {
            return GetResultsFromResponse(artistSearchResults.Results);
        }

        if (searchInfo.Name.HasDiacritics())
        {
            // Try again using the search with an accented characters query
            var artistAccentsSearchResults = await _musicBrainzQuery.FindArtistsAsync($"artistaccent:\"{searchInfo.Name}\"", null, null, false, cancellationToken)
                .ConfigureAwait(false);
            if (artistAccentsSearchResults.Results.Count > 0)
            {
                return GetResultsFromResponse(artistAccentsSearchResults.Results);
            }
        }

        return Enumerable.Empty<RemoteSearchResult>();
    }

    private IEnumerable<RemoteSearchResult> GetResultsFromResponse(IEnumerable<ISearchResult<IArtist>>? releaseSearchResults)
    {
        if (releaseSearchResults is null)
        {
            yield break;
        }

        foreach (var result in releaseSearchResults)
        {
            yield return GetResultFromResponse(result.Item);
        }
    }

    private RemoteSearchResult GetResultFromResponse(IArtist artist)
    {
        var searchResult = new RemoteSearchResult
        {
            Name = artist.Name,
            ProductionYear = artist.LifeSpan?.Begin?.Year,
            PremiereDate = artist.LifeSpan?.Begin?.NearestDate,
            SearchProviderName = Name,
        };

        searchResult.SetProviderId(MetadataProvider.MusicBrainzArtist, artist.Id.ToString());

        return searchResult;
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<MusicArtist> { Item = new MusicArtist() };

        var musicBrainzId = info.GetMusicBrainzArtistId();

        if (string.IsNullOrWhiteSpace(musicBrainzId))
        {
            var searchResults = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);

            var singleResult = searchResults.FirstOrDefault();

            if (singleResult is not null)
            {
                musicBrainzId = singleResult.GetProviderId(MetadataProvider.MusicBrainzArtist);
                result.Item.Overview = singleResult.Overview;

                if (Plugin.Instance!.Configuration.ReplaceArtistName)
                {
                    result.Item.Name = singleResult.Name;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(musicBrainzId))
        {
            result.HasMetadata = true;
            result.Item.SetProviderId(MetadataProvider.MusicBrainzArtist, musicBrainzId);
        }

        return result;
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
