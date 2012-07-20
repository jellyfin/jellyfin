using System.Xml;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Movies.Entities;

namespace MediaBrowser.Movies.Metadata
{
    public class MovieXmlParser : BaseItemXmlParser<Movie>
    {
        protected override void FetchDataFromXmlNode(XmlReader reader, Movie item)
        {
            switch (reader.Name)
            {
                case "TMDbId":
                    item.TmdbId = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "IMDB":
                case "IMDbId":
                    string IMDbId = reader.ReadElementContentAsString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(IMDbId))
                    {
                        item.ImdbId = IMDbId;
                    }
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
