using System;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Model.Entities;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Metadata
{
    public class SeriesXmlParser : BaseItemXmlParser<Series>
    {
        protected async override Task FetchDataFromXmlNode(XmlReader reader, Series item)
        {
            switch (reader.Name)
            {
                case "id":
                    string id = await reader.ReadElementContentAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        item.SetProviderId(MetadataProviders.Tvdb, id);
                    }
                    break;

                case "Airs_DayOfWeek":
                    {
                        string day = await reader.ReadElementContentAsStringAsync();

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
                    item.AirTime = await reader.ReadElementContentAsStringAsync();
                    break;

                case "SeriesName":
                    item.Name = await reader.ReadElementContentAsStringAsync();
                    break;

                case "Status":
                    item.Status = await reader.ReadElementContentAsStringAsync();
                    break;

                default:
                    await base.FetchDataFromXmlNode(reader, item);
                    break;
            }
        }
    }
}
