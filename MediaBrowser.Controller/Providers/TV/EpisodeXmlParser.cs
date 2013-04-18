using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Logging;
using System.IO;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{
    /// <summary>
    /// Class EpisodeXmlParser
    /// </summary>
    public class EpisodeXmlParser : BaseItemXmlParser<Episode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeXmlParser" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public EpisodeXmlParser(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Fetches the data from XML node.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
        protected override void FetchDataFromXmlNode(XmlReader reader, Episode item)
        {
            switch (reader.Name)
            {
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

                            var seasonFolder = Path.GetDirectoryName(item.Path);
                            filename = Path.Combine(seasonFolder, "metadata", filename);

                            if (File.Exists(filename))
                            {
                                item.PrimaryImagePath = filename;
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
