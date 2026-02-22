using System.Collections.Generic;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    /// <summary>
    /// The box set xml parser.
    /// </summary>
    public class BoxSetXmlParser : BaseItemXmlParser<BoxSet>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoxSetXmlParser"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{BoxSetXmlParser}"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        public BoxSetXmlParser(ILogger<BoxSetXmlParser> logger, IProviderManager providerManager)
            : base(logger, providerManager)
        {
        }

        /// <inheritdoc />
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<BoxSet> itemResult)
        {
            switch (reader.Name)
            {
                case "CollectionItems":

                    if (!reader.IsEmptyElement)
                    {
                        using (var subReader = reader.ReadSubtree())
                        {
                            FetchFromCollectionItemsNode(subReader, itemResult);
                        }
                    }
                    else
                    {
                        reader.Read();
                    }

                    break;

                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
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

                                    if (child is not null)
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

            item.Item.LinkedChildren = list.ToArray();
        }
    }
}
