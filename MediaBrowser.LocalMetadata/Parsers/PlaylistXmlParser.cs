using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class PlaylistXmlParser : BaseItemXmlParser<Playlist>
    {
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Playlist> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "PlaylistMediaType":
                    {
                        item.PlaylistMediaType = reader.ReadElementContentAsString();

                        break;
                    }

                case "PlaylistItems":

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
                    base.FetchDataFromXmlNode(reader, result);
                    break;
            }
        }

        private void FetchFromCollectionItemsNode(XmlReader reader, Playlist item)
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
                        case "PlaylistItem":
                            {
                                if (reader.IsEmptyElement)
                                {
                                    reader.Read();
                                    continue;
                                }

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

            item.LinkedChildren = list.ToArray();
        }

        public PlaylistXmlParser(ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory, IFileSystem fileSystem) : base(logger, providerManager, xmlReaderSettingsFactory, fileSystem)
        {
        }
    }
}
