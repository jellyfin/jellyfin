using MediaBrowser.Common.Logging;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Resolvers.TV;
using MediaBrowser.Model.Entities;
using System;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{
    /// <summary>
    /// Class SeriesXmlParser
    /// </summary>
    public class SeriesXmlParser : BaseItemXmlParser<Series>
    {
        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Series item)
        {
            switch (reader.Name)
            {
                case "Series":
                    //MB generated metadata is within a "Series" node
                    using (var subTree = reader.ReadSubtree())
                    {
                        subTree.MoveToContent();

                        // Loop through each element
                        while (subTree.Read())
                        {
                            if (subTree.NodeType == XmlNodeType.Element)
                            {
                                FetchDataFromXmlNode(subTree, item);
                            }
                        }

                    }
                    break;

                case "id":
                    string id = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        item.SetProviderId(MetadataProviders.Tvdb, id);
                    }
                    break;

                case "Airs_DayOfWeek":
                    {
                        item.AirDays = TVUtils.GetAirDays(reader.ReadElementContentAsString());
                        break;
                    }

                case "Airs_Time":
                    item.AirTime = reader.ReadElementContentAsString();
                    break;

                case "SeriesName":
                    item.Name = reader.ReadElementContentAsString();
                    break;

                case "Status":
                    {
                        var status = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(status))
                        {
                            SeriesStatus seriesStatus;
                            if (Enum.TryParse(status, true, out seriesStatus))
                            {
                                item.Status = seriesStatus;
                            }
                            else
                            {
                                Logger.LogInfo("Unrecognized series status: " + status);
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
