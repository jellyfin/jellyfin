using System.Net;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Music;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.TV
{
    public class FanArtSeasonProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IServerConfigurationManager _config;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _json;

        public FanArtSeasonProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem, IJsonSerializer json)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _json = json;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "FanArt"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Season;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Backdrop, 
                ImageType.Thumb,
                ImageType.Banner,
                ImageType.Primary
            };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();

            var season = (Season)item;
            var series = season.Series;

            if (series != null)
            {
                var id = series.GetProviderId(MetadataProviders.Tvdb);

                if (!string.IsNullOrEmpty(id) && season.IndexNumber.HasValue)
                {
                    // Bad id entered
                    try
                    {
                        await FanartSeriesProvider.Current.EnsureSeriesJson(id, cancellationToken).ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound)
                        {
                            throw;
                        }
                    }

                    var path = FanartSeriesProvider.Current.GetFanartJsonPath(id);

                    try
                    {
                        int seasonNumber = AdjustForSeriesOffset(series, season.IndexNumber.Value);
                        AddImages(list, seasonNumber, path, cancellationToken);
                    }
                    catch (FileNotFoundException)
                    {
                        // No biggie. Don't blow up
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // No biggie. Don't blow up
                    }
                }
            }

            var language = item.GetPreferredMetadataLanguage();

            var isLanguageEn = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);

            // Sort first by width to prioritize HD versions
            return list.OrderByDescending(i => i.Width ?? 0)
                .ThenByDescending(i =>
                {
                    if (string.Equals(language, i.Language, StringComparison.OrdinalIgnoreCase))
                    {
                        return 3;
                    }
                    if (!isLanguageEn)
                    {
                        if (string.Equals("en", i.Language, StringComparison.OrdinalIgnoreCase))
                        {
                            return 2;
                        }
                    }
                    if (string.IsNullOrEmpty(i.Language))
                    {
                        return isLanguageEn ? 3 : 2;
                    }
                    return 0;
                })
                .ThenByDescending(i => i.CommunityRating ?? 0)
                .ThenByDescending(i => i.VoteCount ?? 0);
        }

        private int AdjustForSeriesOffset(Series series, int seasonNumber)
        {
            var offset = TvdbSeriesProvider.GetSeriesOffset(series.ProviderIds);
            if (offset != null)
                return (int)(seasonNumber + offset);

            return seasonNumber;
        }

        private void AddImages(List<RemoteImageInfo> list, int seasonNumber, string path, CancellationToken cancellationToken)
        {
            var root = _json.DeserializeFromFile<FanartSeriesProvider.RootObject>(path);

            AddImages(list, root, seasonNumber, cancellationToken);
        }

        private void AddImages(List<RemoteImageInfo> list, FanartSeriesProvider.RootObject obj, int seasonNumber, CancellationToken cancellationToken)
        {
            PopulateImages(list, obj.seasonposter, ImageType.Primary, 1000, 1426, seasonNumber);
            PopulateImages(list, obj.seasonbanner, ImageType.Banner, 1000, 185, seasonNumber);
            PopulateImages(list, obj.seasonthumb, ImageType.Thumb, 500, 281, seasonNumber);
            PopulateImages(list, obj.showbackground, ImageType.Backdrop, 1920, 1080, seasonNumber);
        }

        private Regex _regex_http = new Regex("^http://");
        private void PopulateImages(List<RemoteImageInfo> list,
            List<FanartSeriesProvider.Image> images,
            ImageType type,
            int width,
            int height,
            int seasonNumber)
        {
            if (images == null)
            {
                return;
            }

            list.AddRange(images.Select(i =>
            {
                var url = i.url;
                var season = i.season;

                int imageSeasonNumber;

                if (!string.IsNullOrEmpty(url) &&
                    !string.IsNullOrEmpty(season) &&
                    int.TryParse(season, NumberStyles.Any, _usCulture, out imageSeasonNumber) &&
                    seasonNumber == imageSeasonNumber)
                {
                    var likesString = i.likes;
                    int likes;

                    var info = new RemoteImageInfo
                    {
                        RatingType = RatingType.Likes,
                        Type = type,
                        Width = width,
                        Height = height,
                        ProviderName = Name,
                        Url = _regex_http.Replace(url, "https://", 1),
                        Language = i.lang
                    };

                    if (!string.IsNullOrEmpty(likesString) && int.TryParse(likesString, NumberStyles.Any, _usCulture, out likes))
                    {
                        info.CommunityRating = likes;
                    }

                    return info;
                }

                return null;
            }).Where(i => i != null));
        }

        public int Order
        {
            get { return 1; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = FanartArtistProvider.Current.FanArtResourcePool
            });
        }
    }
}
