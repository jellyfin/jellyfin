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
using MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;
using MetaBrainz.MusicBrainz;
using MetaBrainz.MusicBrainz.Interfaces.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// MusicBrainz audio track metadata provider.
/// </summary>
public class MusicBrainzAudioProvider : IRemoteMetadataProvider<Audio, SongInfo>, IHasOrder, IDisposable
{
    private readonly ILogger<MusicBrainzAudioProvider> _logger;
    private Query _musicBrainzQuery;

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicBrainzAudioProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MusicBrainzAudioProvider(ILogger<MusicBrainzAudioProvider> logger)
    {
        _logger = logger;
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
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SongInfo searchInfo, CancellationToken cancellationToken)
    {
        return Task.FromResult(Enumerable.Empty<RemoteSearchResult>());
    }

    /// <inheritdoc />
    public async Task<MetadataResult<Audio>> GetMetadata(SongInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Audio> { Item = new Audio() };

        var recordingId = info.GetProviderId(MetadataProvider.MusicBrainzRecording);
        var albumId = info.GetProviderId(MetadataProvider.MusicBrainzAlbum);

        if (string.IsNullOrWhiteSpace(recordingId))
        {
            return result;
        }

        var recording = await _musicBrainzQuery.LookupRecordingAsync(
            new Guid(recordingId),
            Include.ArtistCredits | Include.Releases,
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        result.HasMetadata = true;
        result.Item.Name = recording.Title;
        result.Item.ProductionYear = recording.FirstReleaseDate?.Year;
        result.Item.PremiereDate = recording.FirstReleaseDate?.NearestDate;

        if (recording.ArtistCredit is { Count: > 0 })
        {
            result.Item.Artists = recording.ArtistCredit.Select(a => a.Name).ToArray();
        }

        var effectiveAlbumId = !string.IsNullOrEmpty(albumId)
            ? albumId
            : recording.Releases?.FirstOrDefault()?.Id.ToString();

        if (!string.IsNullOrEmpty(effectiveAlbumId))
        {
            var fullRelease = await _musicBrainzQuery.LookupReleaseAsync(
                new Guid(effectiveAlbumId),
                Include.Recordings | Include.ArtistCredits | Include.ReleaseGroups,
                cancellationToken).ConfigureAwait(false);

            result.Item.Album = fullRelease.Title;

            if (fullRelease.ArtistCredit is { Count: > 0 })
            {
                result.Item.AlbumArtists = fullRelease.ArtistCredit.Select(a => a.Name).ToArray();
            }

            if (fullRelease.Date is not null)
            {
                result.Item.ProductionYear = fullRelease.Date.Year;
                result.Item.PremiereDate = fullRelease.Date.NearestDate;
            }

            if (fullRelease.Media is not null)
            {
                foreach (var medium in fullRelease.Media)
                {
                    var track = medium.Tracks?.FirstOrDefault(t => t.Recording is not null && t.Recording.Id.Equals(recording.Id));
                    if (track is not null)
                    {
                        result.Item.IndexNumber = track.Position;
                        result.Item.ParentIndexNumber = medium.Position;

                        if (!string.IsNullOrEmpty(track.Title))
                        {
                            result.Item.Name = track.Title;
                        }

                        if (track.ArtistCredit is { Count: > 0 })
                        {
                            result.Item.Artists = track.ArtistCredit.Select(a => a.Name).ToArray();
                        }

                        break;
                    }
                }
            }
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
