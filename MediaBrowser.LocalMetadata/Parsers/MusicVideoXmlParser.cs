using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.LocalMetadata.Parsers
{
    public class MusicVideoXmlParser : BaseItemXmlParser<MusicVideo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemXmlParser{T}" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public MusicVideoXmlParser(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MusicVideo item)
        {
            switch (reader.Name)
            {
                case "Artist":
                    item.Artist = reader.ReadElementContentAsString();
                    break;

                case "Album":
                    item.Album = reader.ReadElementContentAsString();
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
