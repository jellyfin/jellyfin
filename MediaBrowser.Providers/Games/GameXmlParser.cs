using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Games
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class GameXmlParser : BaseItemXmlParser<Game>
    {
        private Task _chaptersTask = null;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public GameXmlParser(ILogger logger)
            : base(logger)
        {
        }

        public async Task FetchAsync(Game item, string metadataFile, CancellationToken cancellationToken)
        {
            _chaptersTask = null;

            Fetch(item, metadataFile, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (_chaptersTask != null)
            {
                await _chaptersTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Game item)
        {
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

                case "NesBox":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.SetProviderId(MetadataProviders.NesBox, val);
                        }
                        break;
                    }

                case "NesBoxRom":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.SetProviderId(MetadataProviders.NesBoxRom, val);
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
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
