using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
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
        /// <param name="logger">Instance of the <see cref="ILogger{BaseNfoParser}"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
        public EpisodeNfoParser(
            ILogger logger,
            IConfigurationManager config,
            IProviderManager providerManager,
            IUserManager userManager,
            IUserDataManager userDataManager,
            IDirectoryService directoryService)
            : base(logger, config, providerManager, userManager, userDataManager, directoryService)
        {
        }

        /// <inheritdoc />
        protected override void Fetch(MetadataResult<Episode> item, string metadataFile, XmlReaderSettings settings, CancellationToken cancellationToken)
        {
            item.ResetPeople();

            var xmlFile = File.ReadAllText(metadataFile);

            var srch = "</episodedetails>";
            var index = xmlFile.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

            var xml = xmlFile;

            if (index != -1)
            {
                xml = xmlFile.Substring(0, index + srch.Length);
                xmlFile = xmlFile.Substring(index + srch.Length);
            }

            // These are not going to be valid xml so no sense in causing the provider to fail and spamming the log with exceptions
            try
            {
                // Extract episode details from the first episodedetails block
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

                // Extract the last episode number from nfo
                // This is needed because XBMC metadata uses multiple episodedetails blocks instead of episodenumberend tag
                while ((index = xmlFile.IndexOf(srch, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    xml = xmlFile.Substring(0, index + srch.Length);
                    xmlFile = xmlFile.Substring(index + srch.Length);

                    using (var stringReader = new StringReader(xml))
                    using (var reader = XmlReader.Create(stringReader, settings))
                    {
                        reader.MoveToContent();

                        if (reader.ReadToDescendant("episode") && int.TryParse(reader.ReadElementContentAsString(), out var num))
                        {
                            item.Item.IndexNumberEnd = Math.Max(num, item.Item.IndexNumberEnd ?? num);
                        }
                    }
                }
            }
            catch (XmlException)
            {
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
                            // int.TryParse is local aware, so it can be problematic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rval))
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
                            // int.TryParse is local aware, so it can be problematic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rval))
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
                            // int.TryParse is local aware, so it can be problematic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rval))
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
                            // int.TryParse is local aware, so it can be problematic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rval))
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
                            // int.TryParse is local aware, so it can be problematic, force us culture
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rval))
                            {
                                item.AirsBeforeEpisodeNumber = rval;
                            }
                        }

                        break;
                    }

                case "showtitle":
                    {
                        var showtitle = reader.ReadElementContentAsString();

                        if (!string.IsNullOrWhiteSpace(showtitle))
                        {
                            item.SeriesName = showtitle;
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
