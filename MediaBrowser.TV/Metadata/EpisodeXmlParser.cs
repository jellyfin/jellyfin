using System;
using System.IO;
using System.Xml;
using MediaBrowser.Controller.Xml;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Metadata
{
    public class EpisodeXmlParser : BaseItemXmlParser<Episode>
    {
        protected override void FetchDataFromXmlNode(XmlReader reader, Episode item)
        {
            switch (reader.Name)
            {
                case "filename":
                    {
                        string filename = reader.ReadElementContentAsString();

                        if (!string.IsNullOrEmpty(filename))
                        {
                            string metadataFolder = Path.GetDirectoryName(item.Path);
                            item.PrimaryImagePath = Path.Combine(metadataFolder, filename);
                        }
                        break;
                    }
                case "EpisodeNumber":
                    item.EpisodeNumber = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "SeasonNumber":
                    item.SeasonNumber = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "EpisodeName":
                    item.Name = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "FirstAired":
                    {
                        item.FirstAired = reader.ReadElementContentAsString() ?? string.Empty;

                        if (!string.IsNullOrEmpty(item.FirstAired))
                        {
                            DateTime airDate;
                            int y = DateTime.TryParse(item.FirstAired, out airDate) ? airDate.Year : -1;
                            if (y > 1850)
                            {
                                item.ProductionYear = y;
                            }
                        }

                        break;
                    }

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
