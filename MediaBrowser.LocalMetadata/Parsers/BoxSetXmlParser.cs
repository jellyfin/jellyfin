using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class BoxSetXmlParser : BaseItemXmlParser<BoxSet>
    {
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<BoxSet> item)
        {
            switch (reader.Name)
            {
                case "CollectionItems":

                    if (!reader.IsEmptyElement)
                    {
                        using (var subReader = reader.ReadSubtree())
                        {
                            FetchFromCollectionItemsNode(subReader, item);
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }

        private void FetchFromCollectionItemsNode(XmlReader reader, MetadataResult<BoxSet> item)
        {
            var list = new List<LinkedChild>();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "CollectionItem":
                            {
                                if (!reader.IsEmptyElement)
                                {
                                    using (var subReader = reader.ReadSubtree())
                                    {
                                        var child = GetLinkedChild(subReader);

                                        if (child != null)
                                        {
                                            list.Add(child);
                                        }
                                    }
                                }
                                else
                                {
                                    reader.Read();
                                }

                                break;
                            }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            item.Item.LinkedChildren = list;
        }

        public BoxSetXmlParser(ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory, IFileSystem fileSystem) : base(logger, providerManager, xmlReaderSettingsFactory, fileSystem)
        {
        }
    }
}
