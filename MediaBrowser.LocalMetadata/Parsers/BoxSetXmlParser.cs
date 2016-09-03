using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Xml;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class BoxSetXmlParser : BaseItemXmlParser<BoxSet>
    {
        public BoxSetXmlParser(ILogger logger, IProviderManager providerManager)
            : base(logger, providerManager)
        {
        }

        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<BoxSet> item)
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

        private void FetchFromCollectionItemsNode(XmlReader reader, MetadataResult<BoxSet> item)
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

            item.Item.LinkedChildren = list;
        }
    }
}
