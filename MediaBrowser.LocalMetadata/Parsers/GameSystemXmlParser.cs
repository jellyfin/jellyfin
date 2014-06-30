using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class GameSystemXmlParser : BaseItemXmlParser<GameSystem>
    {
        public GameSystemXmlParser(ILogger logger)
            : base(logger)
        {
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        public Task FetchAsync(GameSystem item, string metadataFile, CancellationToken cancellationToken)
        {
            Fetch(item, metadataFile, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return _cachedTask;
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, GameSystem item)
        {
            switch (reader.Name)
            {
                case "GameSystem":
                    {
                        var val = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            item.GameSystemName = val;
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


                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
