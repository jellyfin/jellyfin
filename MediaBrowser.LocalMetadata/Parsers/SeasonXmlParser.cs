using System.Xml;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class SeasonXmlParser : BaseItemXmlParser<Season>
    {
        public SeasonXmlParser(ILogger logger, IProviderManager providerManager)
            : base(logger, providerManager)
        {
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="result">The result.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Season> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "SeasonNumber":
                    {
                        var number = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, out num))
                            {
                                item.IndexNumber = num;
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
