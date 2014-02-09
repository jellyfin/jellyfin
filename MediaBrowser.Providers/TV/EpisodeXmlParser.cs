using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class EpisodeXmlParser : BaseItemXmlParser<Episode>
    {
        private List<LocalImageInfo> _imagesFound;

        public EpisodeXmlParser(ILogger logger)
            : base(logger)
        {
        }

        public void Fetch(Episode item, List<LocalImageInfo> images, string metadataFile, CancellationToken cancellationToken)
        {
            _imagesFound = images;

            Fetch(item, metadataFile, cancellationToken);
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Episode item)
        {
            switch (reader.Name)
            {
                case "Chapters":

                    //_chaptersTask = FetchChaptersFromXmlNode(item, reader.ReadSubtree(), _itemRepo, CancellationToken.None);
                    break;

                case "Episode":

                    //MB generated metadata is within an "Episode" node
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

                case "filename":
                    {
                        var filename = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(filename))
                        {
                            // Strip off everything but the filename. Some metadata tools like MetaBrowser v1.0 will have an 'episodes' prefix
                            // even though it's actually using the metadata folder.
                            filename = Path.GetFileName(filename);

                            var parentFolder = Path.GetDirectoryName(item.Path);
                            filename = Path.Combine(parentFolder, "metadata", filename);
                            var file = new FileInfo(filename);

                            if (file.Exists)
                            {
                                _imagesFound.Add(new LocalImageInfo
                                {
                                    Type = ImageType.Primary,
                                    FileInfo = file
                                });
                            }
                        }
                        break;
                    }
                case "SeasonNumber":
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

                case "EpisodeNumber":
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

                case "EpisodeNumberEnd":
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

                case "EpisodeName":
                    {
                        var name = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            item.Name = name;
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
