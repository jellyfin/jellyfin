using System;
using System.Xml;
using MediaBrowser.Controller.Xml;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Metadata
{
    public class SeriesXmlParser : BaseItemXmlParser<Series>
    {
        protected override void FetchDataFromXmlNode(XmlNode node, Series item)
        {
            switch (node.Name)
            {
                case "id":
                    item.TVDBSeriesId = node.InnerText ?? string.Empty;
                    break;

                case "SeriesName":
                    item.Name = node.InnerText ?? string.Empty;
                    break;

                case "Status":
                    item.Status = node.InnerText ?? string.Empty;
                    break;

                case "Runtime":
                    {
                        string text = node.InnerText ?? string.Empty;
                        if (!string.IsNullOrEmpty(text))
                        {

                            int runtime;
                            if (int.TryParse(text.Split(' ')[0], out runtime))
                            {
                                item.RunTime = TimeSpan.FromMinutes(runtime);
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
