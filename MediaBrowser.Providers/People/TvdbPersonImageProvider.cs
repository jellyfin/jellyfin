using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.TV;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.Providers.People
{
    public class TvdbPersonImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IXmlReaderSettingsFactory _xmlSettings;

        public TvdbPersonImageProvider(IServerConfigurationManager config, ILibraryManager libraryManager, IHttpClient httpClient, IFileSystem fileSystem, IXmlReaderSettingsFactory xmlSettings)
        {
            _config = config;
            _libraryManager = libraryManager;
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _xmlSettings = xmlSettings;
        }

        public string Name
        {
            get { return ProviderName; }
        }

        public static string ProviderName
        {
            get { return "TheTVDB"; }
        }

        public bool Supports(IHasImages item)
        {
            return item is Person;
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
            var seriesWithPerson = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { typeof(Series).Name },
                PersonIds = new[] { item.Id.ToString("N") },
                DtoOptions = new DtoOptions(false)
                {
                    EnableImages = false
                }

            }).Cast<Series>()
                .Where(i => TvdbSeriesProvider.IsValidSeries(i.ProviderIds))
                .ToList();

            var infos = seriesWithPerson.Select(i => GetImageFromSeriesData(i, item.Name, cancellationToken))
                .Where(i => i != null)
                .Take(1);

            return Task.FromResult(infos);
        }

        private RemoteImageInfo GetImageFromSeriesData(Series series, string personName, CancellationToken cancellationToken)
        {
            var tvdbPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, series.ProviderIds);

            var actorXmlPath = Path.Combine(tvdbPath, "actors.xml");

            try
            {
                return GetImageInfo(actorXmlPath, personName, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private RemoteImageInfo GetImageInfo(string xmlFile, string personName, CancellationToken cancellationToken)
        {
            var settings = _xmlSettings.Create(false);

            settings.CheckCharacters = false;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreComments = true;

            using (var fileStream = _fileSystem.GetFileStream(xmlFile, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    // Use XmlReader for best performance
                    using (var reader = XmlReader.Create(streamReader, settings))
                    {
                        reader.MoveToContent();
                        reader.Read();

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "Actor":
                                        {
                                            if (reader.IsEmptyElement)
                                            {
                                                reader.Read();
                                                continue;
                                            }
                                            using (var subtree = reader.ReadSubtree())
                                            {
                                                var info = FetchImageInfoFromActorNode(personName, subtree);

                                                if (info != null)
                                                {
                                                    return info;
                                                }
                                            }
                                            break;
                                        }
                                    default:
                                        reader.Skip();
                                        break;
                                }
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Fetches the data from actor node.
        /// </summary>
        /// <param name="personName">Name of the person.</param>
        /// <param name="reader">The reader.</param>
        /// <returns>System.String.</returns>
        private RemoteImageInfo FetchImageInfoFromActorNode(string personName, XmlReader reader)
        {
            string name = null;
            string image = null;

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Name":
                            {
                                name = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                break;
                            }

                        case "Image":
                            {
                                image = (reader.ReadElementContentAsString() ?? string.Empty).Trim();
                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(image) &&
                string.Equals(name, personName, StringComparison.OrdinalIgnoreCase))
            {
                return new RemoteImageInfo
                {
                    Url = TVUtils.BannerUrl + image,
                    Type = ImageType.Primary,
                    ProviderName = Name

                };
            }

            return null;
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
                Url = url
            });
        }
    }
}
