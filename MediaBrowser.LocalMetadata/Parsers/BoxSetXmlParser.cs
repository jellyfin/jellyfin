using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class BoxSetXmlParser : BaseItemXmlParser<BoxSet>
    {
        private readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public BoxSetXmlParser(ILogger logger)
            : base(logger)
        {
        }

        protected override void FetchDataFromXmlNode(XmlReader reader, BoxSet item)
        {
            switch (reader.Name)
            {
                case "CollectionItems":

                    using (var subReader = reader.ReadSubtree())
                    {
                        FetchFromCollectionItemsNode(subReader, item);
                    }
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }

        private void FetchFromCollectionItemsNode(XmlReader reader, BoxSet item)
        {
            reader.MoveToContent();

            var list = new List<LinkedChild>();

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "CollectionItem":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    var child = GetLinkedChild(subReader);

                                    if (child != null)
                                    {
                                        list.Add(child);
                                    }
                                }

                                break;
                            }

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            item.LinkedChildren = list;
        }
    }
}
