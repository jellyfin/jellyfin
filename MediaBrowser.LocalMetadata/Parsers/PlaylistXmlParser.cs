using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class PlaylistXmlParser : BaseItemXmlParser<Playlist>
    {
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Playlist> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "OwnerUserId":
                    {
                        var userId = reader.ReadElementContentAsString();
                        if (!item.Shares.Any(i => string.Equals(userId, i.UserId, StringComparison.OrdinalIgnoreCase)))
                        {
                            item.Shares.Add(new Share
                            {
                                UserId = userId,
                                CanEdit = true
                            });
                        }

                        break;
                    }

                case "PlaylistMediaType":
                    {
                        item.PlaylistMediaType = reader.ReadElementContentAsString();

                        break;
                    }

                case "PlaylistItems":

                    using (var subReader = reader.ReadSubtree())
                    {
                        FetchFromCollectionItemsNode(subReader, item);
                    }
                    break;

                case "Shares":

                    using (var subReader = reader.ReadSubtree())
                    {
                        FetchFromSharesNode(subReader, item);
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
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "PlaylistItem":
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

            item.LinkedChildren = list;
        }

        private void FetchFromSharesNode(XmlReader reader, Playlist item)
        {
            var list = new List<Share>();

            reader.MoveToContent();
            reader.Read();

            // Loop through each element
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "Share":
                            {
                                using (var subReader = reader.ReadSubtree())
                                {
                                    var child = GetShare(subReader);

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

            item.Shares = list;
        }

        public PlaylistXmlParser(ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory, IFileSystem fileSystem) : base(logger, providerManager, xmlReaderSettingsFactory, fileSystem)
        {
        }
    }
}
