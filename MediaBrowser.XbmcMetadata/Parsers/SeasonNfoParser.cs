using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    public class SeasonNfoParser : BaseNfoParser<Season>
    {
        public SeasonNfoParser(ILogger logger, IConfigurationManager config) : base(logger, config)
        {
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        /// <param name="userDataList">The user data list.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Season item, List<UserItemData> userDataList)
        {
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
                    base.FetchDataFromXmlNode(reader, item, userDataList);
                    break;
            }
        }
    }
}
