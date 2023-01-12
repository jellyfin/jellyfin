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
/// Music album image provider for MusicBrainz. It queries coverartarchive.org with a MusicBrainz album id.
/// </summary>
public class MusicBrainzAlbumImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly ILogger<MusicBrainzAlbumImageProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.CamelCaseOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicBrainzAlbumImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
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
                var obj = await httpClient.GetFromJsonAsync<CoverArtArchiveObject>(jsonUri, _jsonOptions, cancellationToken).ConfigureAwait(false);

                if (obj is not null)
                {
                    var images = obj.Images.Select(img => new RemoteImageInfo
                    {
                        ProviderName = Name,
                        Url = img.Image,
                        ThumbnailUrl = img.Thumbnails.Small,
                        Type = ImageType.Primary
                    });
                    list.AddRange(images);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve image for album {Id} (HTTP error {StatusCode})", id, ex.StatusCode);
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

    private class CoverArtArchiveObject
    {
        public CoverArtArchiveObject(List<CoverArtArchiveImage> images, string release)
        {
            Images = images;
            Release = release;
        }

        public List<CoverArtArchiveImage> Images { get; }

        public string Release { get; }
    }

    private class CoverArtArchiveImage
    {
        public CoverArtArchiveImage(List<string> types, bool front, bool back, uint edit, string image, string comment, bool approved, CoverArtArchiveThumbnail thumbnails, string id)
        {
            Types = types;
            Front = front;
            Back = back;
            Edit = edit;
            Image = image;
            Comment = comment;
            Approved = approved;
            Thumbnails = thumbnails;
            Id = id;
        }

        public List<string> Types { get; }

        public bool Front { get; }

        public bool Back { get; }

        public uint Edit { get; }

        public string Image { get; }

        public string Comment { get; }

        public bool Approved { get; }

        public CoverArtArchiveThumbnail Thumbnails { get; }

        public string Id { get; }
    }

    private class CoverArtArchiveThumbnail
    {
        public CoverArtArchiveThumbnail(string large, string small)
        {
            Large = large;
            Small = small;
        }

        public string Large { get; }

        public string Small { get; }
    }
}
