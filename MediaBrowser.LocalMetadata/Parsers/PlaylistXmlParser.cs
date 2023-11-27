using System;
using System.Collections.Generic;
using System.Xml;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    /// <summary>
    /// Playlist xml parser.
    /// </summary>
    public class PlaylistXmlParser : BaseItemXmlParser<Playlist>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistXmlParser"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{PlaylistXmlParser}"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        public PlaylistXmlParser(ILogger<PlaylistXmlParser> logger, IProviderManager providerManager)
            : base(logger, providerManager)
        {
        }

        /// <inheritdoc />
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Playlist> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "PlaylistMediaType":
                    if (Enum.TryParse<MediaType>(reader.ReadNormalizedString(), out var mediaType))
                    {
                        item.PlaylistMediaType = mediaType;
                    }

                    break;
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
                    base.FetchDataFromXmlNode(reader, itemResult);
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

                                if (child is not null)
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
                else
                {
                    reader.Read();
                }
            }

            item.LinkedChildren = list.ToArray();
        }
    }
}
