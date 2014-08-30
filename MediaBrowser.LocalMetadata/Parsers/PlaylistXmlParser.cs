using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Xml;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class PlaylistXmlParser : BaseItemXmlParser<Playlist>
    {
        public PlaylistXmlParser(ILogger logger)
            : base(logger)
        {
        }

        protected override void FetchDataFromXmlNode(XmlReader reader, Playlist item)
        {
            switch (reader.Name)
            {
                case "OwnerUserId":
                    {
                        item.OwnerUserId = reader.ReadElementContentAsString();

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

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }

        private void FetchFromCollectionItemsNode(XmlReader reader, Playlist item)
        {
            reader.MoveToContent();

            var list = new List<LinkedChild>();

            while (reader.Read())
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
                            reader.Skip();
                            break;
                    }
                }
            }

            item.LinkedChildren = list;
        }
    }
}
