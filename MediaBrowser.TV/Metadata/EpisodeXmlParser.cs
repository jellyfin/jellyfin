using System.IO;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Xml;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Metadata
{
    public class EpisodeXmlParser : BaseItemXmlParser<Episode>
    {
        protected override async Task FetchDataFromXmlNode(XmlReader reader, Episode item)
        {
            switch (reader.Name)
            {
                case "filename":
                    {
                        string filename = await reader.ReadElementContentAsStringAsync();

                        if (!string.IsNullOrWhiteSpace(filename))
                        {
                            string seasonFolder = Path.GetDirectoryName(item.Path);
                            item.PrimaryImagePath = Path.Combine(seasonFolder, "metadata", filename);
                        }
                        break;
                    }
                case "SeasonNumber":
                    {
                        string number = await reader.ReadElementContentAsStringAsync();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            item.ParentIndexNumber = int.Parse(number);
                        }
                        break;
                    }

                case "EpisodeNumber":
                    {
                        string number = await reader.ReadElementContentAsStringAsync();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            item.IndexNumber = int.Parse(number);
                        }
                        break;
                    }

                case "EpisodeName":
                    item.Name = await reader.ReadElementContentAsStringAsync();
                    break;

                default:
                    await base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
