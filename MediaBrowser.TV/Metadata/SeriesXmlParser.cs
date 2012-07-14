using System;
using System.Xml;
using MediaBrowser.Controller.Xml;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Metadata
{
    public class SeriesXmlParser : BaseItemXmlParser<Series>
    {
        protected override void FetchDataFromXmlNode(XmlReader reader, Series item)
        {
            switch (reader.Name)
            {
                case "id":
                    item.TVDBSeriesId = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "SeriesName":
                    item.Name = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "Status":
                    item.Status = reader.ReadElementContentAsString() ?? string.Empty;
                    break;

                case "Runtime":
                    {
                        string text = reader.ReadElementContentAsString() ?? string.Empty;
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
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
