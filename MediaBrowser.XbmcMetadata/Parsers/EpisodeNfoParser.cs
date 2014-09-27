using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
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
        private List<LocalImageInfo> _imagesFound;
        private List<ChapterInfo> _chaptersFound;
        private string _xmlPath;

        public EpisodeNfoParser(ILogger logger, IConfigurationManager config) : base(logger, config)
        {
        }

        public void Fetch(Episode item,
            List<UserItemData> userDataList,
            List<LocalImageInfo> images,
            List<ChapterInfo> chapters, 
            string metadataFile, 
            CancellationToken cancellationToken)
        {
            _imagesFound = images;
            _chaptersFound = chapters;
            _xmlPath = metadataFile;

            Fetch(item, userDataList, metadataFile, cancellationToken);
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        /// <param name="userDataList">The user data list.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Episode item, List<UserItemData> userDataList)
        {
            switch (reader.Name)
            {
                //case "Chapters":

                //    _chaptersFound.AddRange(FetchChaptersFromXmlNode(item, reader.ReadSubtree()));
                //    break;

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


                default:
                    base.FetchDataFromXmlNode(reader, item, userDataList);
                    break;
            }
        }
    }
}
