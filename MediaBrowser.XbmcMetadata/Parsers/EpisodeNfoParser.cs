using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.XbmcMetadata.Parsers
{
    /// <summary>
    /// Nfo parser for episodes.
    /// </summary>
    public class EpisodeNfoParser : BaseNfoParser<Episode>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeNfoParser"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="config">the configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public EpisodeNfoParser(ILogger logger, IConfigurationManager config, IProviderManager providerManager)
            : base(logger, config, providerManager)
        {
        }

        /// <inheritdoc />
        protected override void Fetch(MetadataResult<Episode> item, string metadataFile, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            using (var fileStream = File.OpenRead(metadataFile))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                item.ResetPeople();

                var xml = streamReader.ReadToEnd();

                var srch = "</episodedetails>";
                var index = xml.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    xml = xml.Substring(0, index + srch.Length);
                }

                // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
                try
                {
                    using (var stringReader = new StringReader(xml))
                    using (var reader = XmlReader.Create(stringReader, settings))
                    {
                        reader.MoveToContent();
                        reader.Read();

                        // Loop through each element
                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                FetchDataFromXmlNode(reader, item);
                            }
                            else
                            {
                                reader.Read();
                            }
                        }
                    }
                }
                catch (XmlException)
                {
                }
            }
        }

        /// <inheritdoc />
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
                            if (int.TryParse(number, out var num))
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
                            if (int.TryParse(number, out var num))
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
                            if (int.TryParse(number, out var num))
                            {
                                item.IndexNumberEnd = num;
                            }
                        }

                        break;
                    }

                case "airsbefore_episode":
                    {
                        var val = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out var rval))
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
                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out var rval))
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
                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out var rval))
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
                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out var rval))
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
                            // int.TryParse is local aware, so it can be probamatic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out var rval))
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
