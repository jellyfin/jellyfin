using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
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
                // Retrieves all title and plot tags from the rest of the nfo and concatenates them with the first episode
                // This is needed because XBMC metadata uses multiple episodedetails blocks instead of episodenumberend tag
                var name = new StringBuilder(item.Item.Name);
                var overview = new StringBuilder(item.Item.Overview);
                while ((index = xmlFile.IndexOf(srch, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    xml = xmlFile.Substring(0, index + srch.Length);
                    xmlFile = xmlFile.Substring(index + srch.Length);

                    using (var stringReader = new StringReader(xml))
                    using (var reader = XmlReader.Create(stringReader, settings))
                    {
                        reader.MoveToContent();

                        while (!reader.EOF && reader.ReadState == ReadState.Interactive)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                switch (reader.Name)
                                {
                                    case "name":
                                    case "title":
                                    case "localtitle":
                                        name.Append(" / ").Append(reader.ReadElementContentAsString());
                                        break;
                                    case "episode":
                                        {
                                            if (int.TryParse(reader.ReadElementContentAsString(), out var num))
                                            {
                                                item.Item.IndexNumberEnd = Math.Max(num, item.Item.IndexNumberEnd ?? num);
                                            }

                                            break;
                                        }

                                    case "biography":
                                    case "plot":
                                    case "review":
                                        overview.Append(" / ").Append(reader.ReadElementContentAsString());
                                        break;
                                }
                            }

                            reader.Read();
                        }
                    }
                }

                item.Item.Name = name.ToString();
                item.Item.Overview = overview.ToString();
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
                    if (reader.TryReadInt(out var seasonNumber))
                    {
                        item.ParentIndexNumber = seasonNumber;
                    }

                    break;
                case "episode":
                    if (reader.TryReadInt(out var episodeNumber))
                    {
                        item.IndexNumber = episodeNumber;
                    }

                    break;
                case "episodenumberend":
                    if (reader.TryReadInt(out var episodeNumberEnd))
                    {
                        item.IndexNumberEnd = episodeNumberEnd;
                    }

                    break;
                case "airsbefore_episode":
                case "displayepisode":
                    if (reader.TryReadInt(out var airsBeforeEpisode))
                    {
                        item.AirsBeforeEpisodeNumber = airsBeforeEpisode;
                    }

                    break;
                case "airsafter_season":
                case "displayafterseason":
                    if (reader.TryReadInt(out var airsAfterSeason))
                    {
                        item.AirsAfterSeasonNumber = airsAfterSeason;
                    }

                    break;
                case "airsbefore_season":
                case "displayseason":
                    if (reader.TryReadInt(out var airsBeforeSeason))
                    {
                        item.AirsBeforeSeasonNumber = airsBeforeSeason;
                    }

                    break;
                case "showtitle":
                    item.SeriesName = reader.ReadNormalizedString();
                    break;
                default:
                    base.FetchDataFromXmlNode(reader, itemResult);
                    break;
            }
        }
    }
}
