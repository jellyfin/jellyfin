using System.Linq;
using System.Xml;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Model.Entities;
using MediaBrowser.Movies.Entities;

namespace MediaBrowser.Movies.Metadata
{
    public class MovieXmlParser : BaseItemXmlParser<Movie>
    {
        protected override void FetchDataFromXmlNode(XmlNode node, Movie item)
        {
            switch (node.Name)
            {
                case "TMDbId":
                    item.TmdbId = node.InnerText ?? string.Empty;
                    break;

                case "IMDB":
                case "IMDbId":
                    string IMDbId = node.InnerText ?? string.Empty;
                    if (!string.IsNullOrEmpty(IMDbId))
                    {
                        item.ImdbId = IMDbId;
                    }
                    break;

                default:
                    base.FetchDataFromXmlNode(node, item);
                    break;
            }
        }
    }
}
