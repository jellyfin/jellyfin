using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using System;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{
    public class SeriesXmlParser : BaseItemXmlParser<Series>
    {
        protected override void FetchDataFromXmlNode(XmlReader reader, Series item)
        {
            switch (reader.Name)
            {
                case "id":
                    string id = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        item.SetProviderId(MetadataProviders.Tvdb, id);
                    }
                    break;

                case "Airs_DayOfWeek":
                    {
                        string day = reader.ReadElementContentAsString();

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
                    item.AirTime = reader.ReadElementContentAsString();
                    break;

                case "SeriesName":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "Status":
                    item.Status = reader.ReadElementContentAsString();
                    break;

                default:
                    base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
