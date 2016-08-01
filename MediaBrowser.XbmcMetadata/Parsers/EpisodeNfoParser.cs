using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Xml;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    public class EpisodeNfoParser : BaseNfoParser<Episode>
    {
        public EpisodeNfoParser(ILogger logger, IConfigurationManager config) : base(logger, config)
        {
        }

        public void Fetch(MetadataResult<Episode> item,
            List<LocalImageInfo> images,
            string metadataFile, 
            CancellationToken cancellationToken)
        {
            Fetch(item, metadataFile, cancellationToken);
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="itemResult">The item result.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, MetadataResult<Episode> itemResult)
        {
            var item = itemResult.Item;

            switch (reader.Name)
            {
                case "season":
                    {
                        var number = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, out num))
                            {
                                item.ParentIndexNumber = num;
                            }
                        }
                        break;
                    }

                case "episode":
                    {
                        var number = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, out num))
                            {
                                item.IndexNumber = num;
                            }
                        }
                        break;
                    }

                case "episodenumberend":
                    {
                        var number = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            int num;

                            if (int.TryParse(number, out num))
                            {
                                item.IndexNumberEnd = num;
                            }
                        }
                        break;
                    }

                case "absolute_number":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
                            {
                                item.AbsoluteEpisodeNumber = rval;
                            }
                        }

                        break;
                    }
                case "DVD_episodenumber":
                    {
                        var number = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            float num;

                            if (float.TryParse(number, NumberStyles.Any, UsCulture, out num))
                            {
                                item.DvdEpisodeNumber = num;
                            }
                        }
                        break;
                    }

                case "DVD_season":
                    {
                        var number = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(number))
                        {
                            float num;

                            if (float.TryParse(number, NumberStyles.Any, UsCulture, out num))
                            {
                                item.DvdSeasonNumber = Convert.ToInt32(num);
                            }
                        }
                        break;
                    }

                case "airsbefore_episode":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
                            {
                                item.AirsBeforeEpisodeNumber = rval;
                            }
                        }

                        break;
                    }

                case "airsafter_season":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
                            {
                                item.AirsAfterSeasonNumber = rval;
                            }
                        }

                        break;
                    }

                case "airsbefore_season":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
                            {
                                item.AirsBeforeSeasonNumber = rval;
                            }
                        }

                        break;
                    }

                case "displayseason":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
                            {
                                item.AirsBeforeSeasonNumber = rval;
                            }
                        }

                        break;
                    }

                case "displayepisode":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int rval;

                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out rval))
                            {
                                item.AirsBeforeEpisodeNumber = rval;
                            }
                        }

                        break;
                    }


                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }
    }
}
