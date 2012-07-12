using System;
using System.IO;
using System.Xml;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Xml;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Metadata
{
    public class EpisodeXmlParser : BaseItemXmlParser<Episode>
    {
        protected override void FetchDataFromXmlNode(XmlNode node, Episode item)
        {
            switch (node.Name)
            {
                case "filename":
                    {
                        string filename = node.InnerText;

                        if (!string.IsNullOrEmpty(filename))
                        {
                            string metadataFolder = Path.GetDirectoryName(item.Path);
                            item.PrimaryImagePath = Path.Combine(metadataFolder, filename);
                        }
                        break;
                    }
                case "EpisodeNumber":
                    item.EpisodeNumber = node.InnerText ?? string.Empty;
                    break;

                case "SeasonNumber":
                    item.SeasonNumber = node.InnerText ?? string.Empty;
                    break;

                case "EpisodeName":
                    item.Name = node.InnerText ?? string.Empty;
                    break;

                case "FirstAired":
                    {
                        item.FirstAired = node.InnerText ?? string.Empty;

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
                    base.FetchDataFromXmlNode(node, item);
                    break;
            }
        }
    }
}
