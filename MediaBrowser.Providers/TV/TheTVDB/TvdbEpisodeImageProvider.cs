using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CommonIO;

namespace MediaBrowser.Providers.TV
{
    public class TvdbEpisodeImageProvider : IRemoteImageProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;

        public TvdbEpisodeImageProvider(IServerConfigurationManager config, IHttpClient httpClient, IFileSystem fileSystem)
        {
            _config = config;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
        }

        public string Name
        {
            get { return "TheTVDB"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Episode;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(IHasImages item, CancellationToken cancellationToken)
        {
            var episode = (Episode)item;
            var series = episode.Series;

            if (series != null && TvdbSeriesProvider.IsValidSeries(series.ProviderIds))
            {
                // Process images
                var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, series.ProviderIds);
                var indexOffset = TvdbSeriesProvider.GetSeriesOffset(series.ProviderIds) ?? 0;

				var nodes = TvdbEpisodeProvider.Current.GetEpisodeXmlNodes(seriesDataPath, episode.GetLookupInfo());

                var result = nodes.Select(i => GetImageInfo(i, cancellationToken))
                    .Where(i => i != null)
					.ToList();

				return Task.FromResult<IEnumerable<RemoteImageInfo>>(result);
            }

            return Task.FromResult<IEnumerable<RemoteImageInfo>>(new RemoteImageInfo[] { });
        }

		private RemoteImageInfo GetImageInfo(XmlReader reader, CancellationToken cancellationToken)
        {
            var height = 225;
            var width = 400;
            var url = string.Empty;

			// Use XmlReader for best performance
			using (reader)
			{
				reader.MoveToContent();

				// Loop through each element
				while (reader.Read())
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (reader.NodeType == XmlNodeType.Element)
					{
						switch (reader.Name)
						{
						case "thumb_width":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									int rval;

									// int.TryParse is local aware, so it can be probamatic, force us culture
									if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
									{
										width = rval;
									}
								}
								break;
							}

						case "thumb_height":
							{
								var val = reader.ReadElementContentAsString();

								if (!string.IsNullOrWhiteSpace(val))
								{
									int rval;

									// int.TryParse is local aware, so it can be probamatic, force us culture
									if (int.TryParse(val, NumberStyles.Integer, _usCulture, out rval))
									{
										height = rval;
									}
								}
								break;
							}

						case "filename":
							{
								var val = reader.ReadElementContentAsString();
								if (!string.IsNullOrWhiteSpace(val))
								{
									url = TVUtils.BannerUrl + val;
								}
								break;
							}

						default:
							reader.Skip();
							break;
						}
					}
				}
			}

            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            return new RemoteImageInfo
            {
                Width = width,
                Height = height,
                ProviderName = Name,
                Url = url,
                Type = ImageType.Primary
            };
        }

        public int Order
        {
            get { return 0; }
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = TvdbSeriesProvider.Current.TvDbResourcePool
            });
        }
    }
}
