using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Xml;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class MusicVideoXmlParser : BaseVideoXmlParser<MusicVideo>
    {
        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="result">The result.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<MusicVideo> result)
        {
            var item = result.Item;

            switch (reader.Name)
            {
                case "Artist":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            var artists = val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            item.Artists.AddRange(artists);
                        }

                        break;
                    }

                case "Album":
                    item.Album = reader.ReadElementContentAsString();
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, result);
                    break;
            }
        }

        public MusicVideoXmlParser(ILogger logger, IProviderManager providerManager, IXmlReaderSettingsFactory xmlReaderSettingsFactory, IFileSystem fileSystem) : base(logger, providerManager, xmlReaderSettingsFactory, fileSystem)
        {
        }
    }
}
