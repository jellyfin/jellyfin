using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class GameXmlParser : BaseItemXmlParser<Game>
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public GameXmlParser(ILogger logger, IProviderManager providerManager)
            : base(logger, providerManager)
        {
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        public Task FetchAsync(MetadataResult<Game> item, string metadataFile, CancellationToken cancellationToken)
        {
            Fetch(item, metadataFile, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return _cachedTask;
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="result">The result.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Game> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "GameSystem":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.GameSystem = val;
                        }
                        break;
                    }

                case "GamesDbId":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.SetProviderId(MetadataProviders.Gamesdb, val);
                        }
                        break;
                    }

                case "Players":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int num;

                            if (int.TryParse(val, NumberStyles.Integer, _usCulture, out num))
                            {
                                item.PlayersSupported = num;
                            }
                        }
                        break;
                    }


                default:
                    base.FetchDataFromXmlNode(reader, result);
                    break;
            }
        }
    }
}
