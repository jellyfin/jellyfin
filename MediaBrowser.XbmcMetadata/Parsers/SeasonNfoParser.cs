using System.Globalization;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    public class SeasonNfoParser : BaseNfoParser<Season>
    {
        public SeasonNfoParser(ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(logger, config, providerManager)
        {
        }

        /// <inheritdoc />
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
                            if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num))
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
