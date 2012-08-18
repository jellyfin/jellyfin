using System;
using System.Xml;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Model.Entities;
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
                    string id = reader.ReadString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        item.SetProviderId(MetadataProviders.Tvdb, id);
                    }
                    break;

                case "Airs_DayOfWeek":
                    {
                        string day = reader.ReadString();

                        if (!string.IsNullOrWhiteSpace(day))
                        {
                            if (day.Equals("Daily", StringComparison.OrdinalIgnoreCase))
                            {
                                item.AirDays = new DayOfWeek[] { 
                                    DayOfWeek.Sunday,
                                    DayOfWeek.Monday,
                                    DayOfWeek.Tuesday,
                                    DayOfWeek.Wednesday,
                                    DayOfWeek.Thursday,
                                    DayOfWeek.Friday,
                                    DayOfWeek.Saturday
                                };
                            }
                            else
                            {
                                item.AirDays = new DayOfWeek[] { 
                                    (DayOfWeek)Enum.Parse(typeof(DayOfWeek), day, true)
                                };
                            }
                        }

                        break;
                    }

                case "Airs_Time":
                    item.AirTime = reader.ReadString();
                    break;

                case "SeriesName":
                    item.Name = reader.ReadString();
                    break;

                case "Status":
                    item.Status = reader.ReadString();
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
