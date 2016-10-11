using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Globalization;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    public class SeasonNfoParser : BaseNfoParser<Season>
    {
        public SeasonNfoParser(ILogger logger, IConfigurationManager config, IProviderManager providerManager) : base(logger, config, providerManager)
        {
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="itemResult">The item result.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Season> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "seasonnumber":
                    {
                        var number = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out num))
                            {
                                item.IndexNumber = num;
                            }
                        }
                        break;
                    }

                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }
    }
}
