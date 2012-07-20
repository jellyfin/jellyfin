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
                    item.TmdbId = reader.ReadString();
                    break;

                case "IMDB":
                case "IMDbId":
                    string IMDbId = reader.ReadString();
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
