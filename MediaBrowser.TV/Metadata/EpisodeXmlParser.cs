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

                        if (!string.IsNullOrWhiteSpace(filename))
                        {
                            string metadataFolder = Path.GetDirectoryName(item.Path);
                            item.PrimaryImagePath = Path.Combine(metadataFolder, filename);
                        }
                        break;
                    }
                case "EpisodeNumber":
                    string number = reader.ReadElementContentAsString() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(number))
                    {
                        item.IndexNumber = int.Parse(number);
                    }
                    break;

                case "SeasonNumber":
                    item.SeasonNumber = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "EpisodeName":
                    item.Name = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "FirstAired":
                    {
                        string firstAired = reader.ReadElementContentAsString() ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(firstAired))
                        {
                            DateTime airDate;

                            if (DateTime.TryParse(firstAired, out airDate) && airDate.Year > 1850)
                            {
                                item.PremiereDate = airDate;
                                item.ProductionYear = airDate.Year;
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
