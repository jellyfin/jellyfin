#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// Music album cover art provider for MusicBrainz.
/// </summary>
public class MusicBrainzAlbumImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly ILogger<MusicBrainzAlbumImageProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

    public MusicBrainzAlbumImageProvider(IHttpClientFactory httpClientFactory, ILogger<MusicBrainzAlbumImageProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "MusicBrainz";

    /// <inheritdoc />
    // After embedded and fanart
    public int Order => 2;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType>
        {
            ImageType.Primary
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var id = item.GetProviderId(MetadataProvider.MusicBrainzAlbum);
        var list = new List<RemoteImageInfo>();

        if (!string.IsNullOrWhiteSpace(id))
        {
            var jsonUri = $"https://coverartarchive.org/release/{id}";

            var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
            try
            {
                var obj = await httpClient.GetFromJsonAsync<CoverArtArchiveObject>(jsonUri, _jsonOptions, cancellationToken);
                var images = obj.images.Select(img => new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = img.image,
                    ThumbnailUrl = img.thumbnails.small,
                    Type = ImageType.Primary
                });
                list.AddRange(images);
            }
            catch (HttpRequestException ex)
            {
#pragma warning disable CA2254
                _logger.LogWarning($"Failed to retrieve image for album {id} (HTTP error {ex.StatusCode})");
#pragma warning restore CA2254
            }
        }

        return list;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return httpClient.GetAsync(url, cancellationToken);
    }

    /// <inheritdoc />
    public bool Supports(BaseItem item)
        => item is MusicAlbum;

#pragma warning disable SA1300
    public class CoverArtArchiveObject
    {
        public List<CoverArtArchiveImage> images { get; set; }

        public string release { get; set; }
    }

    public class CoverArtArchiveImage
    {
        public List<string> types { get; set; }

        public bool front { get; set; }

        public bool back { get; set; }

        public uint edit { get; set; }

        public string image { get; set; }

        public string comment { get; set; }

        public bool approved { get; set; }

        public CoverArtArchiveThumbnail thumbnails { get; set; }

        public string id { get; set; }
    }

    public class CoverArtArchiveThumbnail
    {
        public string large { get; set; }

        public string small { get; set; }
    }
#pragma warning restore SA1300
}
