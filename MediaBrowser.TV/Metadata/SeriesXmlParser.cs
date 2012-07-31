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
                    item.TvdbId = reader.ReadString();
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

                case "Runtime":
                    {
                        string text = reader.ReadString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {

                            int runtime;
                            if (int.TryParse(text.Split(' ')[0], out runtime))
                            {
                                item.RunTimeInMilliseconds = runtime * 60000;
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
